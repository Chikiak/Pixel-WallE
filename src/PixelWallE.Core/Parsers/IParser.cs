using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST;
using PixelWallE.Core.Tokens;

namespace PixelWallE.Core.Parsers;

public interface IParser
{
    Result<ProgramStmt> Parse(IReadOnlyList<Token> tokens);
}