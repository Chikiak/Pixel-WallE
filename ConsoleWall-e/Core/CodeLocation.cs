namespace ConsoleWall_e.Core;

public readonly struct CodeLocation(int line, int column)
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}