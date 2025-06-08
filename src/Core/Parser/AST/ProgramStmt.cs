using Core.Common;
using Core.Parser.AST.Stmts;

namespace Core.Parser.AST;

public class ProgramStmt(List<Stmt> statements, CodeLocation location) : ASTNode(location)
{
    public IReadOnlyList<Stmt> Statements { get; private set; } = statements.AsReadOnly();

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitProgramStmt(this);
    }
}