using Core.Common;
using Core.Tokens;

namespace Core.Lexing;

public interface ILexer
{
    Result<IReadOnlyList<Token>> ScanTokens();
}