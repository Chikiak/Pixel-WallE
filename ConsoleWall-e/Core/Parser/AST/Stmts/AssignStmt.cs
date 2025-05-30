using ConsoleWall_e.Core.Parser.AST.Exprs;

namespace ConsoleWall_e.Core.Parser.AST.Stmts;

public class AssignStmt(string name, Expr value, CodeLocation location) : Stmt(location)
{
    public string Name { get; private set; } = name;
    public Expr Value { get; private set; } = value;


    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitAssignStmt(this);
    }
}