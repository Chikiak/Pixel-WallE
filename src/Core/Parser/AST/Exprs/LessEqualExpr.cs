using Core.Common;

namespace Core.Parser.AST.Exprs;

public class LessEqualExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLessEqualExpr(this);
    }
}