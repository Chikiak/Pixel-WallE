using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parser.AST.Exprs;

public class BangExpr(Expr right, CodeLocation location) : UnaryExpr(right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitBangExpr(this);
    }
}