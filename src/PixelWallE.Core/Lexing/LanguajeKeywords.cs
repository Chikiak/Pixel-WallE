using PixelWallE.Core.Tokens;

namespace PixelWallE.Core.Lexing;

public static class LanguajeKeywords
{
    private static readonly IReadOnlyDictionary<string, TokenType> KeywordsMap = new Dictionary<string, TokenType>
    {
        { "Spawn", TokenType.Spawn },
        { "Color", TokenType.Color },
        { "Size", TokenType.Size },
        { "DrawLine", TokenType.DrawLine },
        { "DrawCircle", TokenType.DrawCircle },
        { "DrawRectangle", TokenType.DrawRectangle },
        { "Fill", TokenType.Fill },
        { "and", TokenType.And },
        { "or", TokenType.Or },
        { "true", TokenType.True },
        { "false", TokenType.False },
        { "GoTo", TokenType.GoTo }
    };

    public static TokenType GetKeywordType(string text)
    {
        return KeywordsMap.GetValueOrDefault(text, TokenType.Identifier);
    }

    public static bool IsKeyword(string text)
    {
        return KeywordsMap.ContainsKey(text);
    }
}