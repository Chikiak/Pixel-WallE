using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Errors;
using ConsoleWall_e.Core.Parser.AST;
using ConsoleWall_e.Core.Parser.AST.Stmts;
using ConsoleWall_e.Core.Tokens;

namespace ConsoleWall_e.Core.Parser;

public class Parser : IParser
{
    private IReadOnlyList<Token> _tokens;
    private int _current = 0;
    private readonly List<Error> _errors = new();

    public Result<ProgramStmt> Parse(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _errors.Clear();
        var statements = new List<Stmt>();

        while (!IsAtEnd())
        {
            // Ignorar ENDL vacíos entre sentencias
            while (Peek().Is(TokenType.Endl) && !IsAtEnd()) Advance();
            if (IsAtEnd()) break;
            statements.Add(ParseStmt());
        }

        if (_errors.Any()) return Result<ProgramStmt>.Failure(_errors);

        var programLocation = tokens.FirstOrDefault()?.Location ?? new CodeLocation(1, 1);
        if (statements.Any()) programLocation = statements.First().Location;
        return Result<ProgramStmt>.Success(new ProgramStmt(statements, programLocation));
    }

    private Stmt ParseStmt()
    {
        throw new NotImplementedException();
    }

    private Token Peek()
    {
        return _tokens[_current];
    }

    private Token Previous()
    {
        return _tokens[_current - 1];
    }

    private bool IsAtEnd()
    {
        return Peek().Is(TokenType.EOF);
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Is(type);
    }

    private bool Match(params TokenType[] types)
    {
        if (Peek().IsAny(types))
        {
            Advance();
            return true;
        }

        return false;
    }
}