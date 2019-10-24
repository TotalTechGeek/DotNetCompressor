using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Resources;
using System.Text;

namespace NetCompressor
{
    class Program
    {
        const string EXTERNAL_COMPRESSOR = "SharpCompress";

        //special flags that are program wide.
        private static bool flaggedSevenZipForDeletion;
        private static string appToBeCompressed;
        private static string outputFile;
        private static List<string> dllInstructions = new List<string>();
        private static string possibleMessage = "", possibleAssembly = "";
        private static bool gzOr7z = true;

        private static ResourceManager manager = new ResourceManager("NetCompressor.Properties.Resources", Assembly.GetExecutingAssembly());

        /// <summary>
        /// Compresses the Application and writes it into the Resource file.
        /// Also generates some code related to the Application launching.
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        private static string CompressApplication(ResourceWriter writer)
        {
            //this is what the application is called in the resource file.
            const string APPLICATION_NAME = "app";
            
            //generates a temporary file to compress into.
            FileStream stream = File.Open(outputFile + "temp_c.dat", FileMode.Create);

            //opens the input file (which has been merged with some other dlls).
            FileStream stream2 = File.OpenRead(outputFile + "_temp");

            //sets the mode to follow when it generates the code. It swaps between G-Zip and LZMA.
            string mode;
            long appSize = stream2.Length;
            Stream gStream = null;
            
            if(gzOr7z)
            {
                // lzipstream
                gStream = new SharpCompress.Compressors.LZMA.LZipStream(stream, SharpCompress.Compressors.CompressionMode.Compress);
                mode = "SharpCompress.Compressors.LZMA.LZipStream(memStr, SharpCompress.Compressors.CompressionMode.Decompress)";
            }
            else
            {
                //gzip
                gStream = new GZipStream(stream, CompressionLevel.Optimal);
                mode = "GZipStream(memStr, CompressionMode.Decompress)";
            }

            //compresses, not the most efficient way to compress, I should buffer it, but it doesn't matter.
            while (stream2.Position < stream2.Length)
            {
                gStream.WriteByte((byte)stream2.ReadByte());
            }
            
  
            //closes each of the streams.
            gStream.Close();
            stream.Close(); //closing this one manually because some compression libraries don't have the wrapper close the stream passed into it.
            stream2.Close();

            //add the resource to the file.
            writer.AddResource(APPLICATION_NAME, File.ReadAllBytes(outputFile + "temp_c.dat"));

            //add the code.
            string code = manager.GetString("AppMethod").Replace("%appname%", APPLICATION_NAME).Replace("%appsize%", "" + appSize).Replace("%mode%", mode);
            
            return code;
        }
        
        /// <summary>
        /// Merges dlls into one file prior to compression.
        /// </summary>
        private static void GetDLLs()
        {
            //adds the output file to the dll instructions.
            dllInstructions.Insert(0,"/out:" + outputFile +"_temp");

            //adds the input file to the dll instructions.
            dllInstructions.Insert(1, appToBeCompressed);


            if (dllInstructions.Count != 2) //if there have been instructions added prior, that means dlls have been passed in, repack it.
            {
                new ILRepacking.ILRepack(new ILRepacking.RepackOptions(new ILRepacking.CommandLine(dllInstructions))).Repack();
            }
            else //repacking isn't necessary if there are no dlls. just rename the file.
            {
                if (File.Exists(outputFile + "_temp")) File.Delete(outputFile + "_temp");
                File.Copy(appToBeCompressed, outputFile + "_temp");
            }
          
          
        }

        /// <summary>
        /// Gets the ending stub of the source code generated.
        /// </summary>
        /// <returns></returns>
        private static string GetEnd()
        {
            return manager.GetString("AppMethodEnd");
        }

        /// <summary>
        /// Gets any messages that may have been passed in.
        /// </summary>
        /// <returns></returns>
        private static string GetMessage()
        {
            string result = "";
            //if there is a message, and it exists, add it into the code.
            if(possibleMessage != "")
            {
                if(File.Exists(possibleMessage))
                {
                    //I am lazy, I didn't want to do a bunch of work to get text onto a line, so I base-64 encoded it.
                    string text = File.ReadAllText(possibleMessage);
                    text = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                    result += "String msg = \"" + text +  "\";\n";
                    result += "msg = Encoding.UTF8.GetString(Convert.FromBase64String(msg));\n";
                    result += "Console.WriteLine(msg);\n";   
                }
            }

            return result;
        }


        /// <summary>
        /// Gets the code in the main method.
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        private static string GetMainMethodCode(ResourceWriter writer)
        {
            string result = "";


            GetDLLs();
            result += GetMessage() + "\n";
            result += CompressApplication(writer) + "\n";
            result += GetEnd() + "\n";
            


            return result;
        }



        /// <summary>
        /// Goes through all the auxillary methods to generate the source code to load the application after it is compressed.
        /// </summary>
        /// <returns></returns>
        private static string GenerateSource()
        {
            string result = "";

            //usings.
            result += "using System;\n";
            result += "using System.Reflection;\n";
            result += "using System.Resources;\n";
            result += "using System.Text;\n";
            result += "using System.IO;\n";
            result += "using System.IO.Compression;\n";

            result += GetAssemblyInfo() + "\n";

            //create a resource writer, so we can embed the compressed materials.
            ResourceWriter writer = new ResourceWriter(File.Open("resource.resources", FileMode.Create));

            //add the basic structure.
            result += "namespace CompressedApp\n{\nclass Program\n{\n[STAThread]\nstatic void Main(string[] args)\n{\n" + GetMainMethodCode(writer)  +  "\n}\n}\n}\n";


            //close the resource writer.
            writer.Close();


            return result;
        }

        /// <summary>
        /// Get the assembly information.
        /// This is all the stuff like "Trademark year"
        /// "Product Name"
        /// "Company"
        /// </summary>
        /// <returns></returns>
        private static string GetAssemblyInfo()
        {
            string result = "";

            if(possibleAssembly != "" && File.Exists(possibleAssembly))
            result += File.ReadAllText(possibleAssembly);

            return result;
        }


        /// <summary>
        /// Used to save an extracted icon from a file.
        /// This isn't absolutely necessary for this application, but I think it is useful.
        /// </summary>
        /// <param name="SourceBitmap"></param>
        /// <param name="FilePath"></param>
        private static void SaveAsIcon(Bitmap SourceBitmap, string FilePath)
        {
            FileStream FS = new FileStream(FilePath, FileMode.Create);
            // ICO header
            FS.WriteByte(0); FS.WriteByte(0);
            FS.WriteByte(1); FS.WriteByte(0);
            FS.WriteByte(1); FS.WriteByte(0);

            // Image size
            FS.WriteByte((byte)SourceBitmap.Width);
            FS.WriteByte((byte)SourceBitmap.Height);
            // Palette
            FS.WriteByte(0);
            // Reserved
            FS.WriteByte(0);
            // Number of color planes
            FS.WriteByte(0); FS.WriteByte(0);
            // Bits per pixel
            FS.WriteByte(32); FS.WriteByte(0);

            // Data size, will be written after the data
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);

            // Offset to image data, fixed at 22
            FS.WriteByte(22);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);

            // Writing actual data
            SourceBitmap.Save(FS, ImageFormat.Png);

            // Getting data length (file length minus header)
            long Len = FS.Length - 22;

            // Write it in the correct place
            FS.Seek(14, SeekOrigin.Begin);
            FS.WriteByte((byte)Len);
            FS.WriteByte((byte)(Len >> 8));

            FS.Close();
        }

        /// <summary>
        /// The main method.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            int i = 0;
           

            //This requires quite a bit of explaining.
            //As I was developing this application, I decided it would probably be best if the SevenZipSharp file were 
            //contained as a resource in this application.
            //One of the reasons is that it makes it easier to spit out, to be merged with other applications, even if it isn't in the same directory.
            //it also makes the application completely self contained (once you pack in the ILRepacker.dll with this application.
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler((s, a) =>
            {
                
                if(a.Name.Substring(0, a.Name.IndexOf(",")) == EXTERNAL_COMPRESSOR)
                {

                    //If the file exists, it doesn't need to be spit out, but if it doesn't, put the file in the directory, and flag it for deletion afterwards.
                    if (!File.Exists(Directory.GetCurrentDirectory() + "\\" + EXTERNAL_COMPRESSOR  + ".dll"))
                    {
                        flaggedSevenZipForDeletion = true;
                        File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\" + EXTERNAL_COMPRESSOR + ".dll", (byte[])manager.GetObject(EXTERNAL_COMPRESSOR));
                        

                    }
                    return Assembly.LoadFile(Directory.GetCurrentDirectory() + "\\"+ EXTERNAL_COMPRESSOR + ".dll");
                }


                return null;
            });


            //flag variables.
            string iconSelected = "";

            bool displayCode = false; //display the code?
            bool exclude7zip = false; //exclude the 7-zip dll from being injected?
            bool messageMode = false; //is there a message for the console?
            bool assemblyMode = false; //include information about the executable (meta data stuff).
            bool ripIconMode = false; //rip the icon from the source executable?
            bool getIcon = false; //get an icon that already exists?
            bool export = false; //export an empty assembly file? (or the sevensharpzip file).
            if (args.Length < 2)
            {
                //information.
                Console.WriteLine("[Input Exe | -e] [Output File] (-gz | -lz | -lz0) (-w) (-d) (-i) (-m [text file]) (-a [file]) (dlls) ... ");
                Console.WriteLine("-e (Exports a file with all the default compilation tags for you to modify. You could also use this option to export the SevenSharpZip.dll file)");
                Console.WriteLine("-gz (Sets it to GZip mode)");
                Console.WriteLine("-lz (Sets it to Lzma mode) (default)");
                Console.WriteLine("-lz0 (Lzma mode, but doesn't package in the 7-zip dll)");
                Console.WriteLine("-w (Specifies to not launch a console at the start of the application. Windows Mode)");
                Console.WriteLine("-d (Displays the code generated.)");
                Console.WriteLine("-rip (attempts to rip the icon from the exe it is compressing. Do not use with -i).");
                Console.WriteLine("-m [text file] (allows you to add a text file to display in the console). ");
                Console.WriteLine("-a [file] (allows you to write in some C# tag code. This is recommended for adding in your compilation tags).");
                Console.WriteLine("-i [icon file] (allows you to add an icon to your application of your choice. Do not use with -rip).");

                Environment.Exit(0);
            }

            //compiler parameters.
            System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();


            foreach (String word in args)
            {
                if(i==0)
                {
                   if(word.Trim() == "-e") //export mode flag
                    {
                        export = true;
                    }
                    else
                    appToBeCompressed = word; //sets the input executable.
                }
                else if(i==1)
                {
                    outputFile = word;  //sets the output executable.

                    //if the extension is a dll, then export the SevenZipSharp file.
                    if (export && word.EndsWith(".dll"))
                    {
                        File.WriteAllBytes(word, (byte[])manager.GetObject(EXTERNAL_COMPRESSOR));
                        return;
                    }
                    //otherwise export the blank assembly file.
                }
                else
                {

                    //goes through all the flag data.
                    //read about the flags above.
                    if (export)
                    {
                        //nothing.
                    }
                    else
                    if(getIcon) // a flag to read the next argument as the icon file.
                    {
                        iconSelected = word;
                        getIcon = false;
                    }
                    else
                    if (assemblyMode) // a flag to read the next argument as an assembly information file.
                    {
                        possibleAssembly = word;
                        assemblyMode = false;
                    }
                    else
                    if (messageMode) // a flag to read the next argument as a message file.
                    {
                        possibleMessage = word;
                        messageMode = false;
                    }
                    else if (word.Trim() == "-rip") // command flag to rip icon from previous exe (to the best of its ability).
                    {
                        ripIconMode = true;
                    }
                    else if(word.Trim() == "-i") // command flag to grab an icon.
                    {
                        getIcon = true;
                    }
                    else if(word.Trim() == "-a") // command flag to grab an assembly file.
                    {
                        assemblyMode = true;
                    }
                    else if(word.Trim() == "-m") // command flag to grab a message file.
                    {
                        messageMode = true;
                    }
                    else
                    if (word.Trim() == "-lz0") // command flag to use lzma compression but not embed the file.
                    {
                        gzOr7z = true;
                        exclude7zip = true;
                       
                    }
                    else
                    if(word.Trim() == "-lz") // command flag to use lzma compression (default)
                    {
                        gzOr7z = true;
                        exclude7zip = false;
                    }
                    else
                    if (word.Trim() == "-gz") // command flag to use gzip compression.
                    {
                        gzOr7z = false;
                        exclude7zip = true;
                    }
                    else
                    if (word.Trim() == "-w") //command flag to compile as a window executable, and not as a console executable.
                    {
                        parameters.CompilerOptions = "/target:winexe";
                    }
                    else
                    if( word.Trim() == "-d") // command flag to display the generated code. 
                    {
                        displayCode = true;
                    }
                    else // otherwise, just assume it is a dll file.
                    {
                        dllInstructions.Add("" + word.Trim() + "");
                    }


                }


                i++;
            }

            //if the export flag is toggled on, then print out the blank C# tag assembly file. 
            if(export)
            {
                File.WriteAllText(outputFile, manager.GetString("Assembly"));
                return;
            }

            
            //Provide a compiler version (this might be refractored out later on).
            var providerOptions = new Dictionary<string, string>();
            providerOptions.Add("CompilerVersion", "v4.0");
            

            //create a code compiler.
            using (CSharpCodeProvider codeProvider = new CSharpCodeProvider(providerOptions))
            {
                //compiler
                ICodeCompiler icc = codeProvider.CreateCompiler();
            
                //tell it to make an exe.
                parameters.GenerateExecutable = true;
                
                //if it supports resource files on this platform, go through with the generation.
                if (codeProvider.Supports(GeneratorSupport.Resources))
                {
                    parameters.EmbeddedResources.Add("resource.resources");
                    string source = GenerateSource(); //generate the code and embed dlls.

                    //if the displayCode flag is on, print out the source code generated.
                    if(displayCode)
                    {
                        Console.WriteLine(source);
                    }
                    
                    //add the system dll to referenced assemblies.
                    parameters.ReferencedAssemblies.Add("System.dll");

                    //set it to export the assembly.
                    parameters.OutputAssembly = outputFile;

                    //if lzma compression, then add the SevenZipSharp dll as a requirement.
                    if (gzOr7z)
                    parameters.ReferencedAssemblies.Add(EXTERNAL_COMPRESSOR + ".dll");


                    //check icon flags.
                    if (iconSelected != "" && File.Exists(iconSelected))
                    {
                        parameters.CompilerOptions += " /win32icon:" + iconSelected;
                    }
                    else
                    if (ripIconMode)
                    {
                        //weird hack to get the icon to extract from original exe.
                        Icon iconFromExe = Icon.ExtractAssociatedIcon(appToBeCompressed);
                        if (iconFromExe != null)
                        {
                            //convert to bitmap.
                            Bitmap iconBitmapFromExe = iconFromExe.ToBitmap();

                            //use a custom method to save as icon. C#'s built in method is not very good.
                            SaveAsIcon(new Bitmap(iconBitmapFromExe, new Size((int)(iconBitmapFromExe.Size.Width * 1.0f), (int)(iconBitmapFromExe.Size.Height * 1.0f))), appToBeCompressed + "icon.ico");
                            
                            //set it to use that icon.
                            parameters.CompilerOptions += " /win32icon:" + appToBeCompressed + "icon.ico";
                        }
                    }

                    //try compiling, print out errors if it can't.
                    CompilerResults results = icc.CompileAssemblyFromSource(parameters, source);
                    
                    //if it compiled, move it to be a temp file, it might need more repackaging. 
                    if(File.Exists(outputFile))
                    {
                        if (File.Exists(outputFile + "_temp")) File.Delete(outputFile +"_temp");
                        File.Move(outputFile, outputFile + "_temp");
                    }

                    //if 7-zip is not exluded, inject the dll into it.
                    if (!exclude7zip)
                    {
                        //clears prior instructions, and embeds the 7zip dll for lzma decompression.
                        dllInstructions.Clear(); 
                        dllInstructions.Add("/out:" + outputFile + "");
                        dllInstructions.Add(outputFile + "_temp");
                        dllInstructions.Add(EXTERNAL_COMPRESSOR + ".dll");
                        
                        new ILRepacking.ILRepack(new ILRepacking.RepackOptions(new ILRepacking.CommandLine(dllInstructions))).Repack();
                       
                    }
                    else
                    {
                        //otherwise, move it to be the final exe. No need to repackage.
                        if (File.Exists(outputFile)) File.Delete(outputFile);
                        File.Copy(outputFile + "_temp", outputFile);
                    }

                    //Delete some of the temporary compilation files.
                    File.Delete(outputFile + "temp_c.dat");
                    File.Delete("resource.resources");
                    File.Delete(outputFile + "_temp");

                    //it only needs to delete this if the icon was ripped in the first place.
                    if (ripIconMode)
                        File.Delete(appToBeCompressed + "icon.ico");


                    //print out any errors in the compilation process.
                    if (results.Errors.Count > 0)
                    {
                        //loops thorugh all the issues.
                        foreach (CompilerError CompErr in results.Errors)
                        {
                            
                                        Console.WriteLine("Line number " + CompErr.Line +
                                        ", Error Number: " + CompErr.ErrorNumber +
                                        ", '" + CompErr.ErrorText + ";" +
                                        Environment.NewLine + Environment.NewLine);
                        }
                    }


                    //this is a hack to delete the SevenZipSharp dll.
                    //it deletes the dll after the application closes, so that we don't have to worry about the assembly being used by the application.
                    if (flaggedSevenZipForDeletion)
                    {
                        //ping something random, give it a timeout, bam.
                        ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/C ping 1.1.1.1 -n 1 -w 250 > Nul & Del " + EXTERNAL_COMPRESSOR + ".dll");
                        
                        //make this process invisible.
                        info.CreateNoWindow = true;
                        info.UseShellExecute = false;
                        info.RedirectStandardOutput = true;
                        info.RedirectStandardError = true;

                        //start the process.
                        var proc = new Process();
                        proc.StartInfo = info;
                        proc.Start();
                    }

                }
                else
                {
                    //stop here if it doesn't support resources.
                }

            }
            
        }
    }
}
