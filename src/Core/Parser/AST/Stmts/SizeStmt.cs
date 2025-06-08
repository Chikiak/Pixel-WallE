using Core.Common;
using Core.Parser.AST.Exprs;

namespace Core.Parser.AST.Stmts;

public class SizeStmt(Expr sizeExpr, CodeLocation location) : Stmt(location)
{
    public Expr SizeExpr { get; private set; } = sizeExpr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitSizeStmt(this);
    }
}