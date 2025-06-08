namespace Core.Common;

public readonly struct CodeLocation(int line, int column) : IEquatable<CodeLocation>, IComparable<CodeLocation>
{
    public int Line { get; } = line;
    public int Column { get; } = column;

    public bool Equals(CodeLocation other)
    {
        return Line == other.Line && Column == other.Column;
    }

    public override bool Equals(object? obj)
    {
        return obj is CodeLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Line, Column);
    }

    public int CompareTo(CodeLocation other)
    {
        var lineComparison = Line.CompareTo(other.Line);
        return lineComparison != 0 ? lineComparison : Column.CompareTo(other.Column);
    }

    public override string ToString()
    {
        return $"({Line}, {Column})";
    }

    public static bool operator ==(CodeLocation left, CodeLocation right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CodeLocation left, CodeLocation right)
    {
        return !(left == right);
    }

    public static bool operator <(CodeLocation left, CodeLocation right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(CodeLocation left, CodeLocation right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(CodeLocation left, CodeLocation right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(CodeLocation left, CodeLocation right)
    {
        return left.CompareTo(right) >= 0;
    }
}