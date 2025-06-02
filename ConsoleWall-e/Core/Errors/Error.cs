namespace ConsoleWall_e.Core.Errors;

public abstract class Error(string message)
{
    public string Message { get; } = message;

    public abstract override string ToString();
}

public class ImportError(string message) : Error(message)
{
    public override string ToString()
    {
        return $"Error: {Message}";
    }
}