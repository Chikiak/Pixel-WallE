namespace ConsoleWall_e.Core.Tokens;

public static class TokenUtils
{
    public static Token? FindFirst(this IEnumerable<Token> tokens, Func<Token, bool> predicate)
    {
        return tokens.FirstOrDefault(predicate);
    }

    public static IEnumerable<Token> OfType(this IEnumerable<Token> tokens, TokenType type)
    {
        return tokens.Where(t => t.Is(type));
    }

    public static IEnumerable<Token> ExceptTypes(this IEnumerable<Token> tokens, params TokenType[] types)
    {
        return tokens.Where(t => !t.IsAny(types));
    }
}