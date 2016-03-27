### Welcome

This is a small project I took up and completed (for the most part) in one night. 

This program can pack .NET DLLs with executables, and compress them to much smaller sizes. The compression ratios are pretty decent, and seem to outperform competing .NET Packers (especially when you pack the executable with both provided compression algorithms).

I created this for my projects that use large libraries that take up a lot of space, and have many dlls. I wanted to reduce the size of the files (if possible), and be able to pack everything into a single standalone exe. 

There were two libraries used to complete this program. 

SevenZipSharp and IL-Repack.

I used IL-Repacks Library Nuget package, and I simply linked in the SevenZipSharp dll. 

GZipCompression is recommended for small files, and LZMA is recommended for larger files. Because LZMA requires an extra dll to be packaged in, you can optionally pack the LZMA executable again to reduce the file size even further (and it gets excellent compression ratios).  

------

### Features

* Supply and Overwrite C# Assembly Information (File Attributes). 
* Rip Icons from the Source Executable for your Final Executable.
* Supply your own Icons.
* GZip and LZMA compression.
* Completely Standalone. 
* Provide a console message that pops up with the compressed C# Exe. 
* Easy DLL Merging.
* Extra commands provided for convenience.


------

### Start Up Commands.

All the commands will be listed when you type 

```
NetCompressor
```

in the command prompt. 


Here's how you'd compress a HelloWorld C# Console Application (it's a small file, so let's use gzip).
```
NetCompressor Hello.exe Hello_compressed.exe -gz
```

Here's how you'd compress a larger application with dlls. (Since it's a bigger project, let's use lzma).

```
NetCompressor BigProject.exe CompressedProject.exe A.dll B.dll C.dll
```


If you wanted to, you could run a second pass with Gzip over the CompressedProject, and it'd compress further.

```
NetCompressor CompressedProject.exe CompressedProject2.exe -gz
```


I will try to add more documentation soon.

--------

### Downloads

Check the releases tab, my releases will be packed with this application.

---

### Compilation Instructions

Download SevenZipSharp.dll from somewhere. There might be a NuGet package for it. Add it to your project references.

Install the NuGet package for IL-Repack (Library). 

Plug the source code into your project, as well as the txt files (put them into the Properties/Resources, along with the SevenZipSharp.dll file).

I will try to provide a full VS setup soon. I need to clean up my project first though.  
