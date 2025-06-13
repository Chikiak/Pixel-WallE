using PixelWallE.Core.Common;
using PixelWallE.Core.Parser.AST;
using PixelWallE.Core.Tokens;

namespace PixelWallE.Core.Parser;

public interface IParser
{
    Result<ProgramStmt> Parse(IReadOnlyList<Token> tokens);
}