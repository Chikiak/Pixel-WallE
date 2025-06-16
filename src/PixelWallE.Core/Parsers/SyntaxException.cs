namespace PixelWallE.Core.Parsers;

public class SyntaxException(string message) : Exception
{
    public string Message { get; } = message;
}