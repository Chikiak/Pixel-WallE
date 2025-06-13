using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parser.AST.Exprs;

public class CallExpr(string calledFunction, List<Expr> arguments, CodeLocation location) : Expr(location)
{
    public string CalledFunction { get; private set; } = calledFunction;
    public IReadOnlyList<Expr> Arguments { get; private set; } = arguments.AsReadOnly();

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitCallExpr(this);
    }
}