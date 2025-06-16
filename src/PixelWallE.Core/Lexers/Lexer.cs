using System.Globalization;
using PixelWallE.Core.Common;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Tokens;

namespace PixelWallE.Core.Lexers;

public class Lexer : ILexer
{
    private readonly string _source;
    private readonly List<Token> _tokens;
    private readonly List<Error> _errors;
    
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;
    private int _column = 1; // Columna actual en la línea
    private int _startColumn = 1; // Columna donde inicia el token actual

    private CodeLocation CurrentLocation => new(_line, _column);
    private CodeLocation TokenLocation => new(_line, _startColumn);

    public Lexer(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _tokens = new List<Token>();
        _errors = new List<Error>();
        _line = 1;
        _column = 1;
    }

    public Result<IReadOnlyList<Token>> ScanTokens()
    {
        try
        {
            while (!IsAtEnd())
            {
                BeginToken();
                ScanToken();
            }

            // Agregar token EOF
            _tokens.Add(TokenFactory.CreateEOF(CurrentLocation));

            return _errors.Any()
                ? Result<IReadOnlyList<Token>>.Failure(_errors)
                : Result<IReadOnlyList<Token>>.Success(_tokens.AsReadOnly());
        }
        catch (Exception ex)
        {
            var error = new LexicalError(CurrentLocation, $"Error inesperado en el lexer: {ex.Message}");
            return Result<IReadOnlyList<Token>>.Failure(error);
        }
    }

    private void BeginToken()
    {
        _start = _current;
        _startColumn = _column;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '#':
                ScanComment();
                break;
            case '\n':
                AddToken(TokenType.Endl);
                NewLine();
                break;
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '[': AddToken(TokenType.LeftBracket); break;
            case ']': AddToken(TokenType.RightBracket); break;
            case ',': AddToken(TokenType.Comma); break;
            case '-': AddToken(TokenType.Minus); break;
            case '+': AddToken(TokenType.Plus); break;
            case '/': AddToken(TokenType.Slash); break;
            case '%': AddToken(TokenType.Modulo); break;
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
                ScanLessOrAssign();
                break;
            case '&':
                if (Match('&')) AddToken(TokenType.And);
                break;
            case '|':
                if (Match('|')) AddToken(TokenType.And);
                break;
            // Ignorar espacios en blanco
            case ' ':
            case '\r':
            case '\t':
                break;
            case '"':
                ScanString();
                break;
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
                    AddError($"Unexpected character: {c}");
                }
                break;
        }
    }

    private void ScanLessOrAssign()
    {
        if (Match('='))
            AddToken(TokenType.LessEqual);
        else if (Match('-'))
            AddToken(TokenType.Assign);
        else
            AddToken(TokenType.Less);
    }


    private void ScanComment()
    {
        while (Peek() != '\n' && !IsAtEnd()) Advance();
    }

    private void ScanIdentifier()
    {
        while (IsAlphaNumericOrDash(Peek())) Advance();

        var text = GetCurrentTokenText();
        var type = LanguajeKeywords.GetKeywordType(text);
        if (type == TokenType.False)
        {
            IntegerOrBool value = false;
            AddToken(type, value);
            return;
        }

        if (type == TokenType.True)
        {
            IntegerOrBool value = true;
            AddToken(type, value);
            return;
        }
        AddToken(type);
    }


    private string GetCurrentTokenText()
    {
        return _source.Substring(_start, _current - _start);
    }

    private void ScanNumber()
    {
        while (IsDigit(Peek())) Advance();
        string numberString = _source.Substring(_start, _current - _start);
        if (int.TryParse(numberString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            IntegerOrBool bvalue = value;
            AddToken(TokenType.Number, bvalue);
        }
        else
        {
            AddError($"Número inválido: {numberString}");
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
            AddError($"Unfinished string");
            return;
        }

        Advance(); // Consumir el " de cierre.

        string value = _source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.String, value);
    }

    private void NewLine()
    {
        _line++;
        _column = 1;
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

    private void AddError(string message)
    {
        _errors.Add(new LexicalError(TokenLocation, message));
    }
}