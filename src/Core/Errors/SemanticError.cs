using Core.Common;

namespace Core.Errors;

public class SemanticError(CodeLocation location, string message) : CodeError(ErrorType.Semantic, location, message);