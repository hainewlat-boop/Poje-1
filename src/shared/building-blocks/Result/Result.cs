namespace Platform.BuildingBlocks.Result;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Implements the Result pattern for explicit error handling without exceptions.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Cannot have success with error");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Cannot have failure without error");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    public static Result<T> Create<T>(T? value) =>
        value is not null ? Success(value) : Failure<T>(Error.NullValue);

    public static Result FirstFailureOrSuccess(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Success();
    }
}

/// <summary>
/// Represents the result of an operation that returns a value of type T.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result");

    public static implicit operator Result<T>(T? value) => Create(value);

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        return IsSuccess
            ? Result.Success(mapper(Value))
            : Result.Failure<TOut>(Error);
    }

    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper)
    {
        return IsSuccess
            ? Result.Success(await mapper(Value))
            : Result.Failure<TOut>(Error);
    }

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        return IsSuccess ? binder(Value) : Result.Failure<TOut>(Error);
    }

    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder)
    {
        return IsSuccess ? await binder(Value) : Result.Failure<TOut>(Error);
    }

    public T GetValueOrDefault(T defaultValue = default!)
    {
        return IsSuccess ? Value : defaultValue;
    }

    public Result<T> Ensure(Func<T, bool> predicate, Error error)
    {
        if (IsFailure)
        {
            return this;
        }

        return predicate(Value) ? this : Result.Failure<T>(error);
    }

    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}
