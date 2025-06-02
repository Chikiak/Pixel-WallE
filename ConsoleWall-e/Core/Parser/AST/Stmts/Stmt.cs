using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Parser.AST.Exprs;

namespace ConsoleWall_e.Core.Parser.AST.Stmts;

public abstract class Stmt(CodeLocation location) : ASTNode(location);

public class LabelStmt(string label, CodeLocation location) : Stmt(location)
{
    public string Label { get; private set; } = label;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLabelStmt(this);
    }
}

public class GoToStmt(string label, Expr condition, CodeLocation location) : Stmt(location)
{
    public string Label { get; private set; } = label;
    public Expr Condition { get; private set; } = condition;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGoToStmt(this);
    }
}

public class ExpressionStmt(Expr expr, CodeLocation location) : Stmt(location)
{
    public Expr Expr { get; private set; } = expr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitExpressionStmt(this);
    }
}