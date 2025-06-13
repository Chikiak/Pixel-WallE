namespace PixelWallE.Core.Parser;

public class SyntaxException(string message) : Exception
{
    public string Message { get; } = message;
}