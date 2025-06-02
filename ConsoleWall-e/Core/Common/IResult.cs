using ConsoleWall_e.Core.Errors;

namespace ConsoleWall_e.Core.Common;

public interface IResult<T>
{
    bool IsSuccess { get; }
    T Value { get; }
    IReadOnlyList<Error> Errors { get; }
}