using Core.Common;
using Core.Parser.AST.Exprs;

namespace Core.Parser.AST.Stmts;

public class ColorStmt(Expr colorExpr, CodeLocation location) : Stmt(location)
{
    public Expr ColorExpr { get; private set; } = colorExpr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitColorStmt(this);
    }
}