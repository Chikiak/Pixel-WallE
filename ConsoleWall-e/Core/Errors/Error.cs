namespace ConsoleWall_e.Core.Errors;

public abstract class Error
{
    public string Message { get; protected set; }
    
    public abstract override string ToString();
}

public class ImportError : Error
{
    public ImportError(string message)
    {
        Message = message;
    }
    public override string ToString()
    {
        return $"Error: {Message}";
    }
}