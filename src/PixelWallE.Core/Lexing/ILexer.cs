using PixelWallE.Core.Common;
using PixelWallE.Core.Tokens;

namespace PixelWallE.Core.Lexing;

public interface ILexer
{
    Result<IReadOnlyList<Token>> ScanTokens();
}