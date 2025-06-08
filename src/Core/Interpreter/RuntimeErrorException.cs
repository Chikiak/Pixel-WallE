using Core.Errors;

namespace Core.Interpreter;

public class RuntimeErrorException(RuntimeError error) : Exception(error.Message)
{
    public RuntimeError runtimeError { get; } = error;
}