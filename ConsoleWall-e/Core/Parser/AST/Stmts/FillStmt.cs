using ConsoleWall_e.Core.Common;

namespace ConsoleWall_e.Core.Parser.AST.Stmts;

public class FillStmt(CodeLocation location) : Stmt(location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitFillStmt(this);
    }
}