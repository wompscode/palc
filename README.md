﻿# ![logo](palc_logo_half.png) palc
**P**hoebe's **A**utomated **L**ibrary **C**onverter  
  
Re-encodes audio files found in a specified directory (and subdirectories of the directory) to a codec of your choice at a bitrate of your choice.  
&nbsp;  
&nbsp;  
`./palc -t 8 -d D:\music_library -c libopus -b 256 -dl` will re-encode every audio file in `D:\music_library` using `libopus` at a bitrate of `256kbps` with a maximum of 8 files converting concurrently, and then delete the source file, leaving only the re-encoded file.  
&nbsp;  
With an AMD Ryzen 5 5600, palc took only `5m15s` to re-encode `1091` songs with `libopus` at `256kbps`.  
&nbsp;  
You will have to install `ffmpeg` on your system, or place `ffmpeg.exe` in the same directory as palc. This can be done using `winget`, `brew`, whatever package manager your system uses or by downloading it from [here](https://ffmpeg.org/download.html).
&nbsp;  
&nbsp;  
Uses the following NuGet packages: [z440.atl.core](https://www.nuget.org/packages/z440.atl.core), [System.CommandLine](https://www.nuget.org/packages/System.CommandLine), [Pastel](https://www.nuget.org/packages/Pastel), [FFMpegCore](https://www.nuget.org/packages/FFMpegCore).  
  
For easy tray-based usage, you can use palc in conjunction with [palc_TrayIcon](https://github.com/wompscode/palc_TrayIcon).