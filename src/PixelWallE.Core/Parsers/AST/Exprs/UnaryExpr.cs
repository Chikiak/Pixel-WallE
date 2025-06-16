using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parsers.AST.Exprs;

public abstract class UnaryExpr(Expr right, CodeLocation location) : Expr(location)
{
    public Expr Right { get; private set; } = right;
}