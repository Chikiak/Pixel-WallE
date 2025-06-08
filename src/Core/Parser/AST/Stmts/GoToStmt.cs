using Core.Common;
using Core.Parser.AST.Exprs;

namespace Core.Parser.AST.Stmts;

public class GoToStmt(string label, Expr condition, CodeLocation location) : Stmt(location)
{
    public string Label { get; private set; } = label;
    public Expr Condition { get; private set; } = condition;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGoToStmt(this);
    }
}