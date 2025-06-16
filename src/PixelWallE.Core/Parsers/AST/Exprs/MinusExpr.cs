using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parsers.AST.Exprs;

public class MinusExpr(Expr right, CodeLocation location) : UnaryExpr(right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitMinusExpr(this);
    }
}