using PixelWallE.Core.Common;

namespace PixelWallE.Core.Errors;

public class CodeError(ErrorType type, CodeLocation location, string message) : Error(type, message)
{
    public CodeLocation Location { get; } = location;

    public override string ToString()
    {
        return $"[línea {Location.Line}, col {Location.Column}] {Type} Error: {Message}";
    }
}