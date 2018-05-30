## Welcome

![Imgur](http://i.imgur.com/7Qpk8RJ.png)


This is a small project I took up and completed (for the most part) in one night. 

The underlying code for this project isn't particularly good, but I've published it because of the utility it provides.

This program can pack .NET DLLs with executables, and compress them to much smaller sizes. The compression ratios are pretty decent, and seem to outperform competing .NET Packers (especially when you pack the executable with both provided compression algorithms).

I created this for my projects with quite a few libraries. I wanted to reduce the size of the files (if possible), and pack everything into a single standalone exe. 

SharpCompress and IL-Repack were used to develop this application, which should be automatically downloaded by NuGet.

GZipCompression is recommended for smaller projects, and LZMA is recommended for larger projects (1.2MB+). 

Because LZMA requires an extra dll to be packaged in, you can optionally pack the LZMA-bundled executable again (with GZip) to reduce the file size even further (and it gets excellent compression ratios).  

------

### Features

* Supply and Overwrite C# Assembly Information (File Attributes). 
* Rip Icons from the Source Executable for your Final Executable.
* Supply your own Icons.
* GZip and LZMA compression.
* Completely Standalone executables (if desired). 
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


Here's how you'd compress a HelloWorld C# Console Application (use GZip, since it's a smaller project):
```
NetCompressor Hello.exe Hello_compressed.exe -gz
```

Here's how you'd compress a larger application with dlls, (use lzma):

```
NetCompressor BigProject.exe CompressedProject.exe A.dll B.dll C.dll
```


Because of the bundled LZMA Binary, you could run a second pass with GZip over the CompressedProject, and it'd compress further:

```
NetCompressor CompressedProject.exe CompressedProject2.exe -gz
```

If you wanted to add assembly information to your executable, you can export an empty assembly document to fill in with this command:

```
NetCompressor -e Application.txt
``` 

(It can be pretty much any name or extension. However, if you type in .dll, it will export the SharpCompress dll instead of the assembly document).

After filling it in, you can pass it in while you're compressing your application.

```
NetCompressor Project.exe Project2.exe -a Application.txt
```

One thing that needs to be mentioned though, if you don't want the console to pop up with your application, you'll need to add a "-w" flag to your command. Here is an example of a more complex command.


```
NetCompressor Project.exe Project2.exe -a Application.txt -w -i Icon.ico Hello.dll World.dll C.dll
```


There are also a few other features to explore.


--------

### Downloads

Check the releases tab, my releases will be packed with this application.

---

### Compilation Instructions

Open the Project Solution. Build.

--- 

### Todo

The SharpCompress binary is ironically rather large. I will need to fork the project and produce slimmer builds. The larger binary is restricting LZMA from completely overtaking GZip. *(Added 5/29/2018)*
