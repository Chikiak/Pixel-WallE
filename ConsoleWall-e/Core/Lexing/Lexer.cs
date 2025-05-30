using System.Globalization;
using ConsoleWall_e.Core.Tokens;

namespace ConsoleWall_e.Core.Lexing;

public class Lexer
{
    private string _source;
    private List<Token> _tokens = [];
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;
    private int _column = 1; // Columna actual en la línea
    private int _startColumn = 1; // Columna donde inicia el token actual
    
    private static Dictionary<string, TokenType> Keywords;
    
    static Lexer()
    {
        Keywords = new Dictionary<string, TokenType>
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
    }
    public Lexer(string code)
    {
        _source = code;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            _startColumn = _column; // Guardar la columna de inicio del token
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EOF, "", null, _line, _column));
        return _tokens;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '#':
                while (Peek() != '\n' && !IsAtEnd()) Advance();
                break;
            case '\n':
                AddToken(TokenType.Endl);
                _line++;
                _column = 1;
                break;
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case ',': AddToken(TokenType.Comma); break;
            case '-': AddToken(TokenType.Minus); break;
            case '+': AddToken(TokenType.Plus); break;
            case '/': AddToken(TokenType.Slash); break;
            case '%': AddToken(TokenType.Modulo); break;
            case '[': AddToken(TokenType.LeftBracket); break;
            case ']': AddToken(TokenType.RightBracket); break;
            case '*': 
                AddToken(Match('*') ? TokenType.Power : TokenType.Star);
                break;
            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);;
                break;
            case '<':
                if (Peek() == '=')
                {
                    Advance();
                    AddToken(TokenType.LessEqual);
                }
                else if (Peek() == '-')
                {
                    Advance();
                    AddToken(TokenType.Assign);
                }
                else
                {
                    AddToken(TokenType.Less);
                }
                break;
            // Ignorar espacios en blanco
            case ' ':
            case '\r':
            case '\t':
                break;
            case '"': ScanString(); break;
            default:
                if (IsDigit(c))
                {
                    ScanNumber();
                }
                else if (IsAlpha(c))
                {
                    ScanIdentifier();
                }
                else
                {
                    //ToDo
                    //Error(_line, _startColumn, $"Carácter inesperado: {c}");
                }
                break;
        }
    }
    private void ScanIdentifier()
    {
        while (IsAlphaNumericOrDash(Peek())) Advance();

        string text = _source.Substring(_start, _current - _start);
        if (Keywords.TryGetValue(text, out TokenType type))
        {
            AddToken(type);
        }
        else
        {
            AddToken(TokenType.Identifier);
        }
    }
    private void ScanNumber()
    {
        while (IsDigit(Peek())) Advance();
        string numberString = _source.Substring(_start, _current - _start);
        if (int.TryParse(numberString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            AddToken(TokenType.Number, value);
        }
        else
        {
            // ToDo
            // Error(_line, _startColumn, $"Número inválido: {numberString}");
        }
    }
    private void ScanString()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            //Acepta strings multilineas
            if (Peek() == '\n')
            {
                _line++;
                _column = 1;
            }
            Advance();
        }

        if (IsAtEnd())
        {
            // ToDo
            // Error(_line, _startColumn, "String sin terminar.");
            return;
        }

        Advance(); // Consumir el " de cierre.

        string value = _source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.String, value);
    }
    private char Peek()
    {
        if (IsAtEnd()) return '\0';
        return _source[_current];
    }
    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (Peek() != expected) return false;
        _current++;
        _column++;
        return true;
    }
    private char Advance()
    {
        _column++;
        return _source[_current++];
    }
    private void AddToken(TokenType type, object? literal = null)
    {
        string text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, literal, _line, _startColumn));
    }
    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z');
    }
    private bool IsAlphaNumericOrDash(char c)
    {
        return IsAlpha(c) || IsDigit(c) || c == '_';
    }
    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }
    private bool IsAtEnd()
    {
        return _current >= _source.Length;
    }
}