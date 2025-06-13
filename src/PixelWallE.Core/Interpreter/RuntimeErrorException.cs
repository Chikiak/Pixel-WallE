using PixelWallE.Core.Errors;

namespace PixelWallE.Core.Interpreter;

public class RuntimeErrorException(RuntimeError error) : Exception(error.Message)
{
    public RuntimeError runtimeError { get; } = error;
}