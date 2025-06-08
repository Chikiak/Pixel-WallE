using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Tokens;

namespace ConsoleWall_e.Core.Parser.AST;

public abstract class ASTNode(CodeLocation location)
{
    public CodeLocation Location { get; protected set; } = location;

    public abstract T Accept<T>(IVisitor<T> visitor);
}