using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST.Exprs;

namespace PixelWallE.Core.Parsers.AST.Stmts;

public class ExpressionStmt(Expr expr, CodeLocation location) : Stmt(location)
{
    public Expr Expr { get; private set; } = expr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitExpressionStmt(this);
    }
}