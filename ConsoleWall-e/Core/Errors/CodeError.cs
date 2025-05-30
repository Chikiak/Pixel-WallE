namespace ConsoleWall_e.Core.Errors;

public class CodeError : Error
{
    public int Line { get; protected set; }
    public int Column { get; protected set; }

    public CodeError(int line, int column, string message)
    {
        Line = line;
        Column = column;
        Message = message;
    }
    public override string ToString()
    {
        return $"[linea {Line}, col {Column}] Error: {Message}";
    }
}