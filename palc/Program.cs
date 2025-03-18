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

using FFMpegCore;
using ATL;

using System.Diagnostics;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Drawing;

namespace palc;

static class Program
{
    private static SemaphoreSlim? semaphore;
    static readonly List<Task<bool>> encodeTasks = new();
    private static readonly Stopwatch timer = new ();
    
    private static int maxTasks = 4;
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
            Prefix = Color.DarkViolet,
            Message = Color.BlueViolet
        },
        Fatal = new()
        {
            Prefix = Color.DarkRed,
            Message = Color.Red
        },
        Verbose = new()
        {
            Prefix = Color.DarkCyan,
            Message = Color.Cyan
        },
        Finish = new()
        {
            Prefix = Color.DarkGreen,
            Message = Color.Green
        }
    };
    
    private static string ElapsedTime(TimeSpan ts) {
        return String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
    }

    private static string Extension(string codec)
    {
        switch (codec)
        {
            case "aac":
                return "aac";
            case "libvorbis":
                return "ogg";
            case "libopus":
                return "opus";
            case "libmp3lame":
                return "mp3";
            case "flac":
                return "flac";
            default:
                return codec;
        }
    }
    
    static int Main(string[] args)
    {
        RootCommand _rootCommand =
        [
            new Option<string>(["--directory", "-d"], "directory to read from"),
            new Option<int>(["--bitrate", "-b"], "bitrate to encode to (default: 256)"),
            new Option<string>(["--codec", "-c"], "codec to re-encode to (default: libopus)"),
            new Option<bool>(["--delete", "-dl"], "delete input files (default: no)"),
            new Option<int>(["--tasks", "-t"], "maximum concurrent encoding tasks (default: 4, recommended: however many threads you have)"),
            new Option<bool>(["--listcodecs", "-lc"], "list available codecs.")
        ];

        _rootCommand.Description = "Recursively convert whole music library from one format to another.";
        _rootCommand.Name = "palc";
        
        _rootCommand.Handler = CommandHandler.Create<string, int, string, bool, int, bool>((directory, bitrate, codec, delete, tasks, listcodecs) =>
        {
            if (tasks != 0) maxTasks = tasks;
            semaphore = new SemaphoreSlim(0, maxTasks);
            
            Logging.Log($"palc - phoebe's automated library converter (max tasks: {maxTasks})", "init", ColourScheme.Init);
            
            if (listcodecs)
            {
                Logging.Log($"palc supports any codec that ffmpeg does technically, however, we recommend these:", "info", ColourScheme.Status);
                Logging.Log("libopus (default), aac, libmp3lame, flac, libvorbis.", "info", ColourScheme.Status);
                return;
            }
            
            if (bitrate == 0) bitrate = 256;
            shouldDelete = delete;
            if (string.IsNullOrEmpty(codec)) codec = "libopus";
            
            if (!string.IsNullOrEmpty(directory))
            {
                string [] x = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                timer.Start();
                foreach (string file in x)
                {
                    FileInfo fInfo = new FileInfo(file);
                    string ext = Path.GetExtension(file);
                    if (ext == ".mp3" || ext == ".flac" || ext == ".opus" || ext == ".ogg" || ext == ".wav" || ext == ".m4a" || ext == ".alac" || ext == ".aac" || ext == ".aiff")
                    {
                        encodeTasks.Add(Task.Run(async () => await Encode(fInfo.FullName, $"{fInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(fInfo.Name)}.{Extension(codec)}", codec, bitrate)));
                        Logging.Log($"{file}: queued for conversion.", "status", ColourScheme.Status);
                    }
                    else
                    {
                        Logging.Log($"{file}: unsupported type.", "status", ColourScheme.Warning);
                    }
                }
                
                semaphore?.Release(maxTasks);
                Task.WaitAll(encodeTasks.ToArray<Task>());
                timer.Stop();
                Logging.Log($"completed in {ElapsedTime(timer.Elapsed)}.", "finished", ColourScheme.Finish);
            }
            else
            {
                Logging.Log($"no directory specified.", "fatal", ColourScheme.Fatal);
            }
        });
        
        return _rootCommand.Invoke(args);
    }
    
    private static async Task<bool> Encode(string input, string output, string codec, int bitrate)
    {
        if (semaphore == null) return false;
        await semaphore.WaitAsync();
        if (Path.GetExtension(input) == Path.GetExtension(output))
        {
            Logging.Log($"file format of {input} is the same as the output format, skipping..", "warning", ColourScheme.Warning);
            semaphore.Release();
            return false;
        }
        Logging.Log($"converting {input} to {codec}..", "status", ColourScheme.Status);
        Stopwatch sw = Stopwatch.StartNew();
        FFMpegArgumentProcessor ffmpeg = FFMpegArguments.FromFileInput(input).OutputToFile(output, false,
            options => options.WithAudioCodec(codec).WithCustomArgument("-map_metadata 0").WithTagVersion(3)
                .WithAudioBitrate(bitrate));
        bool outcome = await ffmpeg.ProcessAsynchronously();
        semaphore.Release();
        
        if (outcome)
        {
            // copy some baseline metadata Just In Case! because ffmpeg likes to NOT COPY METADATA SOMETIMES
            // also in case album art doesn't copy because yeah that happens too
            
            Track newTrack = new Track(output);
            Track? oldTrack = new Track(input);

            if(newTrack.EmbeddedPictures.Count > 0) newTrack.EmbeddedPictures.Clear();
            IList<PictureInfo> pictures = oldTrack.EmbeddedPictures;
            if (pictures.Count > 0)
            {
                foreach(PictureInfo pic in pictures) 
                    newTrack.EmbeddedPictures.Add(pic);
            }

            newTrack.Artist = oldTrack.Artist;
            newTrack.Title = oldTrack.Title;
            newTrack.Album = oldTrack.Album;
            newTrack.AlbumArtist = oldTrack.AlbumArtist;
            newTrack.Year = oldTrack.Year;
            newTrack.Genre = oldTrack.Genre;
            newTrack.TrackNumber = oldTrack.TrackNumber;
            await newTrack.SaveAsync();
            
            oldTrack = null;
            
            if (shouldDelete)
            {
                if(File.Exists(input)) File.Delete(input);
                Logging.Log($"deleted {input}..", "status", ColourScheme.Status);
            }
        }
        sw.Stop();
        Logging.Log(outcome ? $"converted {input}, took {ElapsedTime(sw.Elapsed)}." : $"failed to convert {input}", "status", ColourScheme.Finish);
        return outcome;
    }
}