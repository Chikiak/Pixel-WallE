using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parsers.AST.Exprs;

public class SubtractExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitSubtractExpr(this);
    }
}