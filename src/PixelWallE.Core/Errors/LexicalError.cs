using PixelWallE.Core.Common;

namespace PixelWallE.Core.Errors;

public class LexicalError(CodeLocation location, string message) : CodeError(ErrorType.Lexical, location, message);