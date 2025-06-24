using PixelWallE.Core.Common;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Parsers.AST;
using PixelWallE.Core.Parsers.AST.Exprs;
using PixelWallE.Core.Parsers.AST.Stmts;
using PixelWallE.Core.Tokens;

namespace PixelWallE.Core.Parsers;

public class Parser : IParser
{
    private readonly List<Error> _errors = new();
    private int _current;
    private IReadOnlyList<Token> _tokens;

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
            try
            {
                var stmt = ParseStmt();
                statements.Add(stmt);
            }
            catch (SyntaxException ex)
            {
                AddError($"{ex.Message}");
                Synchronize();
            }
        }

        if (_errors.Any())
            return Result<ProgramStmt>.Failure(_errors);

        var programLocation = tokens.FirstOrDefault()?.Location ?? new CodeLocation(1, 1);
        if (statements.Any())
            programLocation = statements.First().Location;
        return Result<ProgramStmt>.Success(new ProgramStmt(statements, programLocation));
    }

    private Stmt ParseStmt()
    {
        // Comandos de dibujo
        if (Match(TokenType.Spawn)) return ParseSpawnStmt();
        if (Match(TokenType.Respawn)) return ParseRespawnStmt();
        if (Match(TokenType.Color)) return ParseColorStmt();
        if (Match(TokenType.Size)) return ParseSizeStmt();
        if (Match(TokenType.DrawLine)) return ParseDrawLineStmt();
        if (Match(TokenType.DrawCircle)) return ParseDrawCircleStmt();
        if (Match(TokenType.DrawRectangle)) return ParseDrawRectangleStmt();
        if (Match(TokenType.Fill)) return ParseFillStmt();
        if (Match(TokenType.Filling)) return ParseFillingStmt();
        if (Match(TokenType.GoTo)) return ParseGoToStmt();

        // Asignación
        if (Check(TokenType.Identifier) && CheckNext(TokenType.Assign))
            return ParseAssignStmt();

        // Etiquetas (identificadores seguidos de nueva línea o EOF)
        if (Check(TokenType.Identifier) && (CheckNext(TokenType.Endl) || CheckNext(TokenType.EOF)))
            return ParseLabelStmt();
        // Expresión como statement
        return ParseExpressionStmt();
    }

    

    private Token Peek()
    {
        return _tokens[_current];
    }

    private Token Previous()
    {
        if (_current == 0) return _tokens[0];
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

    private bool CheckNext(TokenType type)
    {
        if (_current + 1 >= _tokens.Count) return false;
        return _tokens[_current + 1].Is(type);
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

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
            return Advance();

        var current = Peek();
        throw new SyntaxException(message);
    }

    private void ConsumeEndLine()
    {
        if (Check(TokenType.Endl))
        {
            Advance();
        }
        else if (!IsAtEnd())
        {
            var message = "Se esperaba nueva línea al final del statement";
            throw new SyntaxException(message);
        }
    }

    private void AddError(string message)
    {
        _errors.Add(new SyntaxError(Peek().Location, message, Peek()));
    }

    private void Synchronize()
    {
        Advance();
        while (!IsAtEnd())
        {
            if (Previous().Is(TokenType.Endl)) return;

            if (Peek().IsCommand() || Peek().Is(TokenType.Identifier))
                return;

            Advance();
        }
    }

    #region Statement Parsers

    private SpawnStmt ParseSpawnStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'Spawn'");

        var x = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del primer argumento");
        var y = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después de los argumentos de Spawn");
        ConsumeEndLine();

        return new SpawnStmt(x, y, location);
    }

    private Stmt ParseRespawnStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'Respawn'");

        var x = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del primer argumento");
        var y = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después de los argumentos de Respawn");
        ConsumeEndLine();

        return new RespawnStmt(x, y, location);
    }
    private ColorStmt ParseColorStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'Color'");

        var colorExpr = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después del color");
        ConsumeEndLine();

        return new ColorStmt(colorExpr, location);
    }

    private SizeStmt ParseSizeStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'Size'");

        var sizeExpr = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después del tamaño");
        ConsumeEndLine();

        return new SizeStmt(sizeExpr, location);
    }

    private DrawLineStmt ParseDrawLineStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'DrawLine'");

        var dirX = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del primer argumento");
        var dirY = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del segundo argumento");
        var distance = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después de los argumentos de DrawLine");
        ConsumeEndLine();

        return new DrawLineStmt(dirX, dirY, distance, location);
    }

    private DrawCircleStmt ParseDrawCircleStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'DrawCircle'");

        var dirX = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del primer argumento");
        var dirY = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del segundo argumento");
        var radius = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después de los argumentos de DrawCircle");
        ConsumeEndLine();

        return new DrawCircleStmt(dirX, dirY, radius, location);
    }

    private DrawRectangleStmt ParseDrawRectangleStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'DrawRectangle'");

        var dirX = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del primer argumento");
        var dirY = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del segundo argumento");
        var distance = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del tercer argumento");
        var width = ParseExpression();
        Consume(TokenType.Comma, "Se esperaba ',' después del cuarto argumento");
        var height = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después de los argumentos de DrawRectangle");
        ConsumeEndLine();

        return new DrawRectangleStmt(dirX, dirY, distance, width, height, location);
    }

    private FillStmt ParseFillStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'Fill'");
        Consume(TokenType.RightParen, "Se esperaba ')' después de 'Fill'");
        ConsumeEndLine();

        return new FillStmt(location);
    }
    
    private FillingStmt ParseFillingStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftParen, "Se esperaba '(' después de 'Filling'");

        var boolExpr = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después del booleano de Filling");
        ConsumeEndLine();

        return new FillingStmt(boolExpr, location);
    }

    private GoToStmt ParseGoToStmt()
    {
        var location = Previous().Location;
        Consume(TokenType.LeftBracket, "Se esperaba '[' después de 'GoTo'");

        var labelToken = Consume(TokenType.Identifier, "Se esperaba nombre de etiqueta");
        var label = labelToken.Lexeme;

        Consume(TokenType.RightBracket, "Se esperaba ']' después del nombre de etiqueta");
        Consume(TokenType.LeftParen, "Se esperaba '(' después de ']'");

        var condition = ParseExpression();

        Consume(TokenType.RightParen, "Se esperaba ')' después de la condición");
        ConsumeEndLine();

        return new GoToStmt(label, condition, location);
    }

    private AssignStmt ParseAssignStmt()
    {
        var nameToken = Consume(TokenType.Identifier, "Se esperaba nombre de variable");
        var location = nameToken.Location;

        Consume(TokenType.Assign, "Se esperaba '<-' en asignación");
        var value = ParseExpression();
        ConsumeEndLine();

        return new AssignStmt(nameToken.Lexeme, value, location);
    }

    private LabelStmt ParseLabelStmt()
    {
        var labelToken = Consume(TokenType.Identifier, "Se esperaba nombre de etiqueta");
        var location = labelToken.Location;
        ConsumeEndLine();

        return new LabelStmt(labelToken.Lexeme, location);
    }

    private ExpressionStmt ParseExpressionStmt()
    {
        var expr = ParseExpression();
        ConsumeEndLine();

        return new ExpressionStmt(expr, expr.Location);
    }

    #endregion

    #region ExpressionParse

    private Expr ParseExpression()
    {
        return ParseLogicalAnd();
    }

    private Expr ParseLogicalAnd()
    {
        var expr = ParseLogicalOr();

        while (Match(TokenType.And))
        {
            var right = ParseLogicalOr();
            expr = new AndExpr(expr, right, expr.Location);
        }

        return expr;
    }

    private Expr ParseLogicalOr()
    {
        var expr = ParseEquality();

        while (Match(TokenType.Or))
        {
            var right = ParseEquality();
            expr = new OrExpr(expr, right, expr.Location);
        }

        return expr;
    }

    private Expr ParseEquality()
    {
        var expr = ParseComparison();

        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            var op = Previous();
            var right = ParseComparison();

            switch (op.Type)
            {
                case TokenType.BangEqual:
                    expr = new BangEqualExpr(expr, right, expr.Location);
                    break;
                case TokenType.EqualEqual:
                    expr = new EqualEqualExpr(expr, right, expr.Location);
                    break;
                default:
                    throw new SyntaxException($"Operador de igualdad no reconocido: {op.Type}");
            }
        }

        return expr;
    }

    private Expr ParseComparison()
    {
        var expr = ParseTerm();

        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous();
            var right = ParseTerm();

            switch (op.Type)
            {
                case TokenType.Greater:
                    expr = new GreaterExpr(expr, right, expr.Location);
                    break;
                case TokenType.GreaterEqual:
                    expr = new GreaterEqualExpr(expr, right, expr.Location);
                    break;
                case TokenType.Less:
                    expr = new LessExpr(expr, right, expr.Location);
                    break;
                case TokenType.LessEqual:
                    expr = new LessEqualExpr(expr, right, expr.Location);
                    break;
                default:
                    throw new SyntaxException($"Operador de comparación no reconocido: {op.Type}");
            }
        }

        return expr;
    }

    private Expr ParseTerm()
    {
        var expr = ParseFactor();

        while (Match(TokenType.Minus, TokenType.Plus))
        {
            var op = Previous();
            var right = ParseFactor();

            switch (op.Type)
            {
                case TokenType.Minus:
                    expr = new SubtractExpr(expr, right, expr.Location);
                    break;
                case TokenType.Plus:
                    expr = new AddExpr(expr, right, expr.Location);
                    break;
                default:
                    throw new SyntaxException($"Operador de término no reconocido: {op.Type}");
            }
        }

        return expr;
    }

    private Expr ParseFactor()
    {
        var expr = ParsePower();

        while (Match(TokenType.Slash, TokenType.Star, TokenType.Modulo))
        {
            var op = Previous();
            var right = ParsePower();

            switch (op.Type)
            {
                case TokenType.Slash:
                    expr = new DivideExpr(expr, right, expr.Location);
                    break;
                case TokenType.Star:
                    expr = new MultiplyExpr(expr, right, expr.Location);
                    break;
                case TokenType.Modulo:
                    expr = new ModuloExpr(expr, right, expr.Location);
                    break;
                default:
                    throw new SyntaxException($"Operador de factor no reconocido: {op.Type}");
            }
        }

        return expr;
    }

    private Expr ParsePower()
    {
        var expr = ParseUnary();

        if (Match(TokenType.Power))
        {
            var right = ParseUnary();
            expr = new PowerExpr(expr, right, expr.Location);
        }

        return expr;
    }

    private Expr ParseUnary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            var op = Previous();
            var right = ParseUnary();

            switch (op.Type)
            {
                case TokenType.Bang:
                    return new BangExpr(right, op.Location);
                case TokenType.Minus:
                    return new MinusExpr(right, op.Location);
                default:
                    throw new SyntaxException($"Operador unario no reconocido: {op.Type}");
            }
        }

        return ParseCall();
    }

    private Expr ParseCall()
    {
        var expr = ParsePrimary();

        if (!Check(TokenType.LeftParen)) return expr;
        if (expr is VariableExpr varExpr)
        {
            Advance(); // Consume '('
            var args = new List<Expr>();

            if (!Check(TokenType.RightParen))
                do
                {
                    args.Add(ParseExpression());
                } while (Match(TokenType.Comma));

            Consume(TokenType.RightParen, "Se esperaba ')' después de los argumentos");
            return new CallExpr(varExpr.Name, args, varExpr.Location);
        }

        return expr;
    }

    private Expr ParsePrimary()
    {
        if (Match(TokenType.Number, TokenType.True, TokenType.False))
        {
            var token = Previous();
            return new LiteralExpr(new LiteralValue(token.Literal, typeof(IntegerOrBool)), token.Location);
        }

        if (Match(TokenType.String))
        {
            var token = Previous();
            return new LiteralExpr(new LiteralValue(token.Literal, typeof(string)), token.Location);
        }

        if (Match(TokenType.Identifier))
        {
            var token = Previous();
            return new VariableExpr(token.Lexeme, token.Location);
        }

        if (Match(TokenType.LeftParen))
        {
            var location = Previous().Location;
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Se esperaba ')' después de la expresión");
            return new GroupExpr(expr, TokenType.LeftParen, location);
        }

        if (Match(TokenType.LeftBracket))
        {
            var location = Previous().Location;
            var expr = ParseExpression();
            Consume(TokenType.RightBracket, "Se esperaba ']' después de la expresión");
            return new GroupExpr(expr, TokenType.LeftBracket, location);
        }

        AddError($"Token inesperado: {Peek().Lexeme}");
        throw new SyntaxException($"Token inesperado: {Peek().Lexeme}");
    }

    #endregion
}