namespace ConsoleWall_e.Core.Errors;

public class CodeError(int line, int column, string message) : Error(message)
{
    public int Line { get; } = line;
    public int Column { get; } = column;

    public override string ToString()
    {
        return $"[linea {Line}, col {Column}] Error: {Message}";
    }
}