using Core.Common;
using Core.Tokens;

namespace Core.Errors;

public class SyntaxError(
    CodeLocation location,
    string message,
    Token? unexpectedToken = null,
    params TokenType[] expectedTokens)
    : CodeError(ErrorType.Syntax, location, message)
{
    public Token? UnexpectedToken { get; } = unexpectedToken;
    public TokenType[] ExpectedTokens { get; } = expectedTokens;

    public override string ToString()
    {
        var baseMessage = base.ToString();
        if (UnexpectedToken != null)
            baseMessage += $"\n  Token encontrado: {UnexpectedToken.Type} ('{UnexpectedToken.Lexeme}')";
        if (ExpectedTokens.Any()) baseMessage += $"\n  Tokens esperados: {string.Join(", ", ExpectedTokens)}";
        return baseMessage;
    }
}