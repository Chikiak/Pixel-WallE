using Core.Common;

namespace Core.Errors;

public class LexicalError(CodeLocation location, string message) : CodeError(ErrorType.Lexical, location, message);