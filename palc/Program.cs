// leave me alone rider please
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable RedundantAssignment
// ReSharper disable RedundantArgumentDefaultValue

/*
 * PALC
 *  Phoebe's Automated Library Converter
 * Automatically re-encodes your music library to your chosen codec at your chosen bitrate.
 */

using static palc.Structs;
using static palc.MiscFunctions;
using static palc.Logging;

using FFMpegCore;
using ATL;

using System.Diagnostics;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace palc;

static class Program
{
    private static readonly string? Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    
    private static SemaphoreSlim? semaphore;
    static readonly List<Task<bool>> encodeTasks = new();
    private static int maxTasks = 4;
    
    private static readonly Stopwatch timer = new ();
    private static bool shouldDelete;
    
    private static readonly ConsoleColourScheme ColourScheme = new()
    {
        Init = new()
        {
            Prefix = Color.DarkSlateBlue,
            Message = Color.SlateBlue
        },
        Warning = new()
        {
            Prefix = Color.DarkOrange,
            Message = Color.Orange
        },
        Status = new()
        {
            Prefix = Color.MediumOrchid,
            Message = Color.Orchid
        },
        Fatal = new()
        {
            Prefix = Color.DarkRed,
            Message = Color.Red
        },
        Verbose = new()
        {
            Prefix = Color.LimeGreen,
            Message = Color.Lime
        },
        Finish = new()
        {
            Prefix = Color.DarkGreen,
            Message = Color.Green
        }
    };
    
    static int Main(string[] args)
    {
        RootCommand _rootCommand =
        [
            new Option<string>(["--directory", "-d"], "directory to read from"),
            new Option<int>(["--bitrate", "-b"], "bitrate to encode to (default: 256)"),
            new Option<string>(["--codec", "-c"], "codec to re-encode to (default: libopus)"),
            new Option<bool>(["--delete", "-dl"], "delete input files (default: no)"),
            new Option<int>(["--tasks", "-t"], "maximum concurrent encoding tasks (default: 4, recommended: however many threads you have)"),
            new Option<string>(["--extension", "-e"], "only look for this extension (default: all audio)"),
            new Option<bool>(["--listcodecs", "-lc"], "list available codecs."),
        ];

        _rootCommand.Description = "Recursively convert whole music library from one format to another.";
        _rootCommand.Name = "palc";
        
        _rootCommand.Handler = CommandHandler.Create<string, int, string, bool, int, string, bool>((directory, bitrate, codec, delete, tasks, extension, listcodecs) =>
        {
            Log($"palc {Version} (max tasks: {maxTasks})", "init", ColourScheme.Init);

            // ffmpeg sanity check
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // going to just trust that users will have ffmpeg on other platforms. easy enough.
                if (InPathOrAppDirectory("ffmpeg.exe") == false)
                {
                    Log("ffmpeg is not in PATH or working directory.", "fatal", ColourScheme.Fatal);
                    Environment.Exit(2);
                    return;
                }
            }
            
            // list all codecs that are recommended for this, but you could technically use anything, but I'm going to rely on the user not doing that because you don't get an app like this to do that.
            if (listcodecs)
            {
                Log($"palc supports any codec that ffmpeg does technically, however, we recommend these:", "info", ColourScheme.Status);
                Log("libopus (default), aac, libmp3lame, flac, libvorbis.", "info", ColourScheme.Status);
                return;
            }

            // set variables
            if (tasks != 0) maxTasks = tasks;
            if (bitrate == 0) bitrate = 256;
            
            shouldDelete = delete;
            if (string.IsNullOrEmpty(codec)) codec = "libopus";
            // if there's a directory, get going
            if (!string.IsNullOrEmpty(directory))
            {
                // init semaphore and start working timer 
                semaphore = new SemaphoreSlim(0, maxTasks);
                timer.Start();
                
                // get all files in directory and the subdirectories in it
                string [] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    FileInfo fInfo = new FileInfo(file); // create FileInfo for the current file
                    string ext = Path.GetExtension(file); // get its extension and check if it's a common music related extension.
                    if (string.IsNullOrEmpty(extension) == false)
                    {
                        if (ext == extension)
                        {
                            // create encode task
                            encodeTasks.Add(Task.Run(async () => fInfo.Directory != null && await Encode(fInfo.Name,fInfo.FullName,$"{Path.GetFileNameWithoutExtension(fInfo.Name)}.{Extension(codec)}", $"{Path.Combine(fInfo.Directory.FullName, $"{Path.GetFileNameWithoutExtension(fInfo.Name)}.{Extension(codec)}")}", codec, bitrate)));
                            Log($"{file}: queued for conversion.", "status", ColourScheme.Status);
                        }
                        else
                        {
                            Log($"{file}: not chosen type.", "status", ColourScheme.Warning);
                        }
                    }
                    else
                    {
                        if (ext == ".mp3" || ext == ".flac" || ext == ".opus" || ext == ".ogg" || ext == ".wav" || ext == ".m4a" || ext == ".alac" || ext == ".aac" || ext == ".aiff")
                        {
                            // create encode task
                            encodeTasks.Add(Task.Run(async () => fInfo.Directory != null && await Encode(fInfo.Name,fInfo.FullName,$"{Path.GetFileNameWithoutExtension(fInfo.Name)}.{Extension(codec)}", $"{Path.Combine(fInfo.Directory.FullName, $"{Path.GetFileNameWithoutExtension(fInfo.Name)}.{Extension(codec)}")}", codec, bitrate)));
                            Log($"{file}: queued for conversion.", "status", ColourScheme.Status);
                        }
                        else
                        {
                            Log($"{file}: unsupported type.", "status", ColourScheme.Warning);
                        }
                    }
                }
                
                semaphore?.Release(maxTasks); // start encode tasks
                Task.WaitAll(encodeTasks.ToArray<Task>()); // wait until they're done
                timer.Stop(); // stop timer
                Log($"completed in {ElapsedTime(timer.Elapsed)}.", "finished", ColourScheme.Finish);
            }
            else
            {
                // no directory lol
                Log($"no directory specified.", "fatal", ColourScheme.Fatal);
                Environment.Exit(1);
            }
        });
        
        return _rootCommand.Invoke(args);
    }
    
    private static async Task<bool> Encode(string inputName, string input, string outputName, string output, string codec, int bitrate)
    {
        // no semaphore? Stop now!
        if (semaphore == null) return false;
        await semaphore.WaitAsync(); // wait for semaphore to say go
        if (Path.GetExtension(input) == Path.GetExtension(output)) // check if file is same output format
        {
            Log($"{inputName}: file format is the same as the output format, skipping..", "warning", ColourScheme.Warning);
            semaphore.Release();
            return false;
        }

        if (File.Exists(output)) // check if output already exists
        {
            Log($"{outputName}: output already exists, skipping..", "warning", ColourScheme.Warning);
            semaphore.Release();
            return false;
        }
        
        // init working timer
        Stopwatch workingTimer = Stopwatch.StartNew();
        
        Log($"{inputName}: re-encoding as {codec}..", "status", ColourScheme.Status);
        FFMpegArgumentProcessor ffmpeg = FFMpegArguments.FromFileInput(input).OutputToFile(output, false,
            options => options.WithAudioCodec(codec).WithCustomArgument("-map_metadata 0").WithTagVersion(3)
                .WithAudioBitrate(bitrate)); // setup ffmpeg
        bool outcome = await ffmpeg.ProcessAsynchronously(); // start ffmpeg
        semaphore.Release(); // release upon finish
        
        if (outcome)
        {
            // clone metadata (including cover art!) just in case ffmpeg failed to copy it
            Track newTrack = new Track(output);
            Track oldTrack = new Track(input);
            oldTrack.CopyMetadataTo(newTrack);
            await newTrack.SaveAsync();
            
            if (shouldDelete)
            {
                if(File.Exists(input)) File.Delete(input);
                Log($"{inputName}: deleted.", "status", ColourScheme.Status);
            }
        }
        // stop working timer
        workingTimer.Stop();
        Log(outcome ? $"{outputName}: re-encoded in {ElapsedTime(workingTimer.Elapsed)}." : $"{inputName}: failed to re-encode.", "status", outcome ? ColourScheme.Verbose : ColourScheme.Fatal); // repurpose verbose as status finish
        return outcome;

    }
}