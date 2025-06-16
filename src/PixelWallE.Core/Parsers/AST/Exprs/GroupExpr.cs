using PixelWallE.Core.Common;
using PixelWallE.Core.Tokens;

namespace PixelWallE.Core.Parsers.AST.Exprs;

public class GroupExpr(Expr expr, TokenType groupType, CodeLocation location) : Expr(location)
{
    public TokenType GroupType { get; private set; } = groupType;
    public Expr Expr { get; private set; } = expr;

    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGroupExpr(this);
    }
}