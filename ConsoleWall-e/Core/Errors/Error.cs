using ConsoleWall_e.Core.Common;

namespace ConsoleWall_e.Core.Errors;

public abstract class Error(ErrorType type, string message)
{
    public string Message { get; protected set; } = message;
    public ErrorType Type { get; protected set; } = type;

    public override string ToString()
    {
        return $"Error: {Message}";
    }
}

public class LexicalError(CodeLocation location, string message) : CodeError(ErrorType.Lexical, location, message);

public class SemanticError(CodeLocation location, string message) : CodeError(ErrorType.Semantic, location, message);

public class RuntimeError(CodeLocation location, string message) : CodeError(ErrorType.Runtime, location, message);

public class ImportError(string message) : Error(ErrorType.Import, message);