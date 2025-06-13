using PixelWallE.Core.Common;

namespace PixelWallE.Core.Errors;

public class RuntimeError(CodeLocation location, string message) : CodeError(ErrorType.Runtime, location, message);