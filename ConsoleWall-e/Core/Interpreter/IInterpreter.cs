using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Parser.AST;

namespace ConsoleWall_e.Core.Interpreter;

public interface IInterpreter
{
    Result<object> Interpret(ProgramStmt program);
}