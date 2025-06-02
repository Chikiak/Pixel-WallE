using ConsoleWall_e.Core.Errors;

namespace ConsoleWall_e.Core.Common;

public class Result<T> : IResult<T>
{
    private readonly T _value;
    private readonly List<Error> _errors;

    public bool IsSuccess => !_errors.Any();
    public T Value => IsSuccess ? _value : throw new InvalidOperationException("Cannot access value of failed result");
    public IReadOnlyList<Error> Errors => _errors.AsReadOnly();

    private Result(T value)
    {
        _value = value;
        _errors = new List<Error>();
    }

    private Result(IEnumerable<Error> errors)
    {
        _value = default;
        _errors = errors.ToList();
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    public static Result<T> Failure(Error error)
    {
        return new Result<T>(new[] { error });
    }

    public static Result<T> Failure(IEnumerable<Error> errors)
    {
        return new Result<T>(errors);
    }

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess ? Result<TNew>.Success(mapper(Value)) : Result<TNew>.Failure(Errors);
    }

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        return IsSuccess ? binder(Value) : Result<TNew>.Failure(Errors);
    }
}