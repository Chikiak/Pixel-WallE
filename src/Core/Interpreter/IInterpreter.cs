using Core.Common;
using Core.Parser.AST;

namespace Core.Interpreter;

public interface IInterpreter
{
    Result<object> Interpret(ProgramStmt program);
}