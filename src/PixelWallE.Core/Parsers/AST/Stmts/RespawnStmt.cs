using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST.Exprs;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class RespawnStmt(Expr x, Expr y, CodeLocation location) : Stmt(location)
{
    public Expr X { get; private set; } = x;
    public Expr Y { get; private set; } = y;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitRespawnStmt(this);
    }
}