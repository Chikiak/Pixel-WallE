using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST.Exprs;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class DrawCircleStmt(Expr dirX, Expr dirY, Expr radius, CodeLocation location)
    : Stmt(location)
{
    public Expr DirX { get; private set; } = dirX;
    public Expr DirY { get; private set; } = dirY;
    public Expr Radius { get; private set; } = radius;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitDrawCircleStmt(this);
    }
}