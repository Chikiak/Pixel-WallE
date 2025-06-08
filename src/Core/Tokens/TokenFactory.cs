using Core.Common;

namespace Core.Tokens;

public static class TokenFactory
{
    public static Token Create(TokenType type, string lexeme, CodeLocation location, object? literal = null)
    {
        return new Token(type, lexeme, literal, location.Line, location.Column);
    }

    public static Token CreateEOF(CodeLocation location)
    {
        return Create(TokenType.EOF, "", location);
    }

    public static Token CreateIdentifier(string name, CodeLocation location)
    {
        return Create(TokenType.Identifier, name, location);
    }

    public static Token CreateNumber(int value, CodeLocation location)
    {
        return Create(TokenType.Number, value.ToString(), location, value);
    }

    public static Token CreateString(string value, CodeLocation location)
    {
        return Create(TokenType.String, $"\"{value}\"", location, value);
    }
}