using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class FillStmt(CodeLocation location) : Stmt(location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitFillStmt(this);
    }
}