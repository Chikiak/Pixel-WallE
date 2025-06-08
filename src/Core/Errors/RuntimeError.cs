using Core.Common;

namespace Core.Errors;

public class RuntimeError(CodeLocation location, string message) : CodeError(ErrorType.Runtime, location, message);