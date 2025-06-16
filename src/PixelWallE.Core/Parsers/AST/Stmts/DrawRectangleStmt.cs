using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST.Exprs;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class DrawRectangleStmt(Expr dirX, Expr dirY, Expr distance, Expr width, Expr height, CodeLocation location)
    : Stmt(location)
{
    public Expr DirX { get; private set; } = dirX;
    public Expr DirY { get; private set; } = dirY;
    public Expr Distance { get; private set; } = distance;
    public Expr Width { get; private set; } = width;
    public Expr Height { get; private set; } = height;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitDrawRectangleStmt(this);
    }
}