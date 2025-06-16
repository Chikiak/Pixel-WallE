using PixelWallE.Core.Common;
using PixelWallE.Core.Tokens;

namespace PixelWallE.Core.Lexers;

public interface ILexer
{
    Result<IReadOnlyList<Token>> ScanTokens();
}