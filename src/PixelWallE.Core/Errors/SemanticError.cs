using PixelWallE.Core.Common;

namespace PixelWallE.Core.Errors;

public class SemanticError(CodeLocation location, string message) : CodeError(ErrorType.Semantic, location, message);