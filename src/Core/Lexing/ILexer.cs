using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Tokens;

namespace ConsoleWall_e.Core.Lexing;

public interface ILexer
{
    Result<IReadOnlyList<Token>> ScanTokens();
}