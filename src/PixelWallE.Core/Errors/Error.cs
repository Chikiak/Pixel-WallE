namespace PixelWallE.Core.Errors;

public abstract class Error(ErrorType type, string message)
{
    public string Message { get; protected set; } = message;
    public ErrorType Type { get; protected set; } = type;

    public override string ToString()
    {
        return $"Error: {Message}";
    }
}