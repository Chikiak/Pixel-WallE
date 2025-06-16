using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST.Exprs;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class SizeStmt(Expr sizeExpr, CodeLocation location) : Stmt(location)
{
    public Expr SizeExpr { get; private set; } = sizeExpr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitSizeStmt(this);
    }
}