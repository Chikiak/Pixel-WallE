using PixelWallE.Core.Common;

namespace PixelWallE.Core.Parsers.AST;

public abstract class ASTNode(CodeLocation location)
{
    public CodeLocation Location { get; protected set; } = location;

    public abstract T Accept<T>(IVisitor<T> visitor);
}