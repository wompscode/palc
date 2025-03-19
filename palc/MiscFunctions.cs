// ReSharper disable AssignmentInsteadOfDiscard
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace palc;

public static class MiscFunctions
{
    // Functions that I just want separated for cleanliness purposes
    public static string ElapsedTime(TimeSpan ts) {
        return String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
    }

    public static bool InPathOrAppDirectory(string fileName)
    {
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, fileName))) return true;
        string path = Environment.GetEnvironmentVariable("Path") ?? string.Empty;
        string[] pathSplit = path.Split(';');
        
        foreach (string pathPart in pathSplit)
        {
            if (File.Exists(Path.Combine(pathPart, fileName)))
            {
                return true;
            }
        }

        return false;
    }
    
    public static string Extension(string codec)
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
}