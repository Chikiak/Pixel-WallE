namespace ConsoleWall_e.Core.Parser.AST.Exprs;

public abstract class UnaryExpr(Expr right, CodeLocation location) : Expr(location)
{
    public Expr Right { get; private set; } = right;
}

public class BangExpr(Expr right, CodeLocation location) : UnaryExpr(right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitBangExpr(this);
    }
}

public class MinusExpr(Expr right, CodeLocation location) : UnaryExpr(right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitMinusExpr(this);
    }
}