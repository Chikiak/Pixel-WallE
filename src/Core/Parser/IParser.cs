using Core.Common;
using Core.Parser.AST;
using Core.Tokens;

namespace Core.Parser;

public interface IParser
{
    Result<ProgramStmt> Parse(IReadOnlyList<Token> tokens);
}