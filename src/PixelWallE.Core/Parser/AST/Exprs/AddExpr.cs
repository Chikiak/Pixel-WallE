using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parser.AST.Exprs;

public class AddExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitAddExpr(this);
    }
}