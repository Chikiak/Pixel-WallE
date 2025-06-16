using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parsers.AST.Exprs;

public class LiteralExpr(LiteralValue value, CodeLocation location) : Expr(location)
{
    public LiteralValue Value { get; private set; } = value;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLiteralExpr(this);
    }
}