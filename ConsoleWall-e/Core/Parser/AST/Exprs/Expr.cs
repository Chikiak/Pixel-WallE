using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Tokens;

namespace ConsoleWall_e.Core.Parser.AST.Exprs;

public abstract class Expr(CodeLocation location) : ASTNode(location);

public sealed record LiteralValue(object? Value, Type Type);

public class LiteralExpr(LiteralValue value, CodeLocation location) : Expr(location)
{
    public LiteralValue Value { get; private set; } = value;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLiteralExpr(this);
    }
}

public class GroupExpr(Expr expr, TokenType groupType, CodeLocation location) : Expr(location)
{
    public TokenType GroupType { get; private set; } = groupType;
    public Expr Expr { get; private set; } = expr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGroupExpr(this);
    }
}

public class CallExpr(string calledFunction, List<Expr> arguments, CodeLocation location) : Expr(location)
{
    public string CalledFunction { get; private set; } = calledFunction;
    public List<Expr> Arguments { get; private set; } = arguments;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitCallExpr(this);
    }
}

public class VariableExpr(string name, CodeLocation location) : Expr(location)
{
    public string Name { get; private set; } = name;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitVariableExpr(this);
    }
}