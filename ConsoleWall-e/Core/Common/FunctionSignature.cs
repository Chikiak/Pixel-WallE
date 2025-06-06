namespace ConsoleWall_e.Core.Common;

public struct FunctionSignature(string name, Type returnType, List<Type> parameterTypes)
{
    public string Name { get; } = name;
    public Type ReturnType { get; } = returnType;
    public List<Type> ParameterTypes { get; } = parameterTypes;

    public override string ToString()
    {
        var paramsStr = string.Join(", ", ParameterTypes.Select(p => p.Name));
        return $"{ReturnType.Name} {Name}({paramsStr})";
    }
}