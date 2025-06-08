using Core.Common;

namespace Core.Parser.AST.Exprs;

public abstract class BinaryExpr(Expr left, Expr right, CodeLocation location) : Expr(location)
{
    public Expr Left { get; private set; } = left;
    public Expr Right { get; private set; } = right;
}

#region Arithmetic

#endregion

#region Comparison

#endregion

#region Logical

#endregion