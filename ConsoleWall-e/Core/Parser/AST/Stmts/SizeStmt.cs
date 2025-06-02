using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Parser.AST.Exprs;

namespace ConsoleWall_e.Core.Parser.AST.Stmts;

public class SizeStmt(Expr sizeExpr, CodeLocation location) : Stmt(location)
{
    public Expr SizeExpr { get; private set; } = sizeExpr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitSizeStmt(this);
    }
}