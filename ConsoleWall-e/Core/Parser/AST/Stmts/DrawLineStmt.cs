using ConsoleWall_e.Core.Parser.AST.Exprs;

namespace ConsoleWall_e.Core.Parser.AST.Stmts;

public class DrawLineStmt(Expr dirX, Expr dirY, Expr distance, CodeLocation location) : Stmt(location)
{
    public Expr DirX { get; private set; } = dirX;
    public Expr DirY { get; private set; } = dirY;
    public Expr Distance { get; private set; } = distance;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitDrawLineStmt(this);
    }
}