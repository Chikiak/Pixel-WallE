using Core.Common;

namespace Core.Parser.AST.Exprs;

public abstract class Expr(CodeLocation location) : ASTNode(location);