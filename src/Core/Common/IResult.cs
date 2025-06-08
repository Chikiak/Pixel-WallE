using Core.Errors;

namespace Core.Common;

public interface IResult<T>
{
    bool IsSuccess { get; }
    T Value { get; }
    IReadOnlyList<Error> Errors { get; }
}