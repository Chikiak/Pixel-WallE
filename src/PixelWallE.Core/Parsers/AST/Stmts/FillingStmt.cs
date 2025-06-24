using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST.Exprs;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class FillingStmt(Expr boolExpr, CodeLocation location) : Stmt(location)
{
    public Expr BoolExpr { get; private set; } = boolExpr;
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitFillingStmt(this);
    }
}