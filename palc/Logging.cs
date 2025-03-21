﻿using Pastel;

// ReSharper disable AssignmentInsteadOfDiscard
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo

namespace palc;
public static class Logging
{
    // taken from phdt, because Consistency is cool and Good!
    public static void Log(string message, string? prefix = null, Structs.ConsoleColourSet? colourScheme = null)
    {
        DateTime now = DateTime.Now;
        string _ = $"{(prefix == null ? $"[unknown]" : $"[{prefix}]")} {now:HH:mm:ss}:";
        if (colourScheme.HasValue)
        {
            _ = _.Pastel(colourScheme.Value.Prefix);
        }
        string __ = $" {message}";
        if (colourScheme.HasValue)
        {
            __ = __.Pastel(colourScheme.Value.Message);
        }
        Console.WriteLine(_+__);
    }
}