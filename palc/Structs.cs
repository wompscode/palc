using System.Drawing;

// ReSharper disable AssignmentInsteadOfDiscard
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace palc;
public static class Structs
{
    public struct ConsoleColourScheme
    {
        public ConsoleColourSet Init { get; init; }
        public ConsoleColourSet Warning { get; init; }
        public ConsoleColourSet Status { get; init; }
        public ConsoleColourSet Fatal { get; init; }
        public ConsoleColourSet Verbose { get; set; }
        public ConsoleColourSet Finish { get; init; }
    }
    public struct ConsoleColourSet
    {
        public Color Prefix { get; init; }
        public Color Message { get; init; }
    }
}