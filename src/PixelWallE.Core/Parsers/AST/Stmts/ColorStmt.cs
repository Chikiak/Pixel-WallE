using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST.Exprs;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class ColorStmt(Expr colorExpr, CodeLocation location) : Stmt(location)
{
    public Expr ColorExpr { get; private set; } = colorExpr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitColorStmt(this);
    }
}