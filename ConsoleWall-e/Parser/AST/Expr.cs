using ConsoleWall_e.Tokens;
using System.Collections.Generic;

namespace ConsoleWall_e.Parser.AST;

public abstract class Expr
{
    public abstract T Accept<T>(IVisitor<T> visitor);
}

public class LiteralExpr(object value) : Expr
{
    public object Value { get; private set; } = value;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLiteralExpr(this);
    }
}

public class UnaryExpr(Token ope, Expr right) : Expr
{
    public Token Operator { get; private set; } = ope;
    public Expr Right { get; private set; } = right;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitUnaryExpr(this);
    }
}

public class BinaryExpr(Token ope, Expr left, Expr right) : Expr
{
    public Token Operator { get; private set; } = ope;
    public Expr Left { get; private set; } = left;
    public Expr Right { get; private set; } = right;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitBinaryExpr(this);
    }
}

public class GroupExpr(Expr expr) : Expr
{
    public Expr Expr { get; private set; } = expr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGroupExpr(this);
    }
}

public class CallExpr(Token calledFunction, List<Expr> arguments) : Expr
{
    public Token CalledFunction { get; private set; } = calledFunction;
    public List<Expr> Arguments { get; private set; } = arguments;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitCallExpr(this);
    }
    
}

public class VariableExpr(Token name) : Expr
{
    public Token Name { get; private set; } = name;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitVariableExpr(this);
    }
}