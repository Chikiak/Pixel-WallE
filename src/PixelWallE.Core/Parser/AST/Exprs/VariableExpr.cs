using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parser.AST.Exprs;

public class VariableExpr(string name, CodeLocation location) : Expr(location)
{
    public string Name { get; private set; } = name;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitVariableExpr(this);
    }
}