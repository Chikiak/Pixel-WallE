using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST.Exprs;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class AssignStmt(string name, Expr value, CodeLocation location) : Stmt(location)
{
    public string Name { get; private set; } = name;
    public Expr Value { get; private set; } = value;


    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitAssignStmt(this);
    }
}