using PixelWallE.Core.Errors;

namespace PixelWallE.Core.Common;

public interface IResult<T>
{
    bool IsSuccess { get; }
    T Value { get; }
    IReadOnlyList<Error> Errors { get; }
}