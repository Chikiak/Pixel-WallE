using Core.Common;

namespace Core.Parser.AST.Exprs;

public class MultiplyExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitMultiplyExpr(this);
    }
}