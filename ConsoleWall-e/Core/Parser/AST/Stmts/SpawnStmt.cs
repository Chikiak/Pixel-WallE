using ConsoleWall_e.Core.Parser.AST.Exprs;

namespace ConsoleWall_e.Core.Parser.AST.Stmts;

public class SpawnStmt(Expr x, Expr y, CodeLocation location) : Stmt(location)
{
    public Expr X { get; private set; } = x;
    public Expr Y { get; private set; } = y;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitSpawnStmt(this);
    }
}