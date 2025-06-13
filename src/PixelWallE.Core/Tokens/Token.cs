using PixelWallE.Core.Common;

namespace PixelWallE.Core.Tokens;

public sealed class Token : IEquatable<Token>
{
    public TokenType Type { get; private set; }
    public string Lexeme { get; private set; }
    public object? Literal { get; private set; }
    public CodeLocation Location { get; private set; }

    public Token(TokenType type, string lexeme, object? literal, int line, int column)
    {
        Type = type;
        Lexeme = lexeme ?? throw new ArgumentNullException(nameof(lexeme));
        Literal = literal;
        Location = new CodeLocation(line, column);
    }

    public bool Is(TokenType type)
    {
        return Type == type;
    }

    public bool IsAny(params TokenType[] types)
    {
        return types.Contains(Type);
    }

    public T? GetLiteral<T>() where T : class
    {
        return Literal as T;
    }

    public T GetLiteralOrDefault<T>(T defaultValue = default)
    {
        return Literal is T value ? value : defaultValue;
    }

    public bool IsBinaryOperator()
    {
        return IsAny(
            TokenType.Plus, TokenType.Minus, TokenType.Star, TokenType.Slash, TokenType.Modulo, TokenType.Power,
            TokenType.EqualEqual, TokenType.BangEqual, TokenType.Greater, TokenType.GreaterEqual,
            TokenType.Less, TokenType.LessEqual, TokenType.And, TokenType.Or
        );
    }

    public bool IsUnaryOperator()
    {
        return IsAny(TokenType.Bang, TokenType.Minus);
    }

    public bool IsLiteral()
    {
        return IsAny(TokenType.Number, TokenType.String, TokenType.True, TokenType.False);
    }

    public bool IsCommand()
    {
        return IsAny(
            TokenType.Spawn, TokenType.Color, TokenType.Size, TokenType.DrawLine,
            TokenType.DrawCircle, TokenType.DrawRectangle, TokenType.Fill
        );
    }

    public override string ToString()
    {
        return $"{Type}('{Lexeme}'{(Literal != null ? $", {Literal}" : "")}) at {Location}";
    }

    public string ToDebugString()
    {
        return
            $"TokenType: {Type}, Lexeme: '{Lexeme}', Literal: {Literal?.ToString() ?? "null"}, Line: {Location.Line}, Column: {Location.Column}";
    }

    public bool Equals(Token? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Type == other.Type &&
               Lexeme == other.Lexeme &&
               Equals(Literal, other.Literal) &&
               Location == other.Location;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Token);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Lexeme, Literal, Location);
    }

    public static bool operator ==(Token? left, Token? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Token? left, Token? right)
    {
        return !Equals(left, right);
    }
}