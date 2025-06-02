using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Parser.AST;
using ConsoleWall_e.Core.Tokens;

namespace ConsoleWall_e.Core.Parser;

public interface IParser
{
    Result<ProgramStmt> Parse(IReadOnlyList<Token> tokens);
}