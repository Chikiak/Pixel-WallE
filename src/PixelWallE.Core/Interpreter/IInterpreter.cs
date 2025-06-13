using PixelWallE.Core.Common;
using PixelWallE.Core.Parser.AST;

namespace PixelWallE.Core.Interpreter;

public interface IInterpreter
{
    Result<object> Interpret(ProgramStmt program);
}