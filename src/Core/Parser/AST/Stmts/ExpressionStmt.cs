using Core.Common;
using Core.Parser.AST.Exprs;

namespace Core.Parser.AST.Stmts;

public class ExpressionStmt(Expr expr, CodeLocation location) : Stmt(location)
{
    public Expr Expr { get; private set; } = expr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitExpressionStmt(this);
    }
}