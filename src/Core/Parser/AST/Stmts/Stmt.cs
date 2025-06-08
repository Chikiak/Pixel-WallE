using Core.Common;

namespace Core.Parser.AST.Stmts;

public abstract class Stmt(CodeLocation location) : ASTNode(location);