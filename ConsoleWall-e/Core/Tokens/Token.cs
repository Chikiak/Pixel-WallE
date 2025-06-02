namespace ConsoleWall_e.Core.Tokens;

public class Token
{
    public TokenType Type { get; private set; }
    public string Lexeme { get; private set; }
    public object? Literal { get; private set; }
    public CodeLocation Location { get; private set; }

    public Token(TokenType type, string lexeme, object? literal, int line, int column)
    {
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Location = new CodeLocation(line, column);
    }

    public override string ToString()
    {
        return $"TokenType:{Type} Lexema: {Lexeme} Literal: {Literal} Line: {Location.Line} Column: {Location.Column}";
    }
}