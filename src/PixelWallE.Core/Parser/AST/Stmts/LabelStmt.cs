using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parser.AST.Stmts;

public class LabelStmt(string label, CodeLocation location) : Stmt(location)
{
    public string Label { get; private set; } = label;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLabelStmt(this);
    }
}