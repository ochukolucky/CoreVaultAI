namespace CoreVault.SharedKernel.Primitives;

/// <summary>
/// Represents the outcome of an operation — success or failure.
/// Use this instead of throwing exceptions for expected failures
/// like validation errors, not-found, or business rule violations.
/// Exceptions are for unexpected system failures only.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot have an error.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// A Result that carries a value on success.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Returns the value if successful.
    /// Throws if accessed on a failure — always check IsSuccess first.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Cannot access Value of a failed result. Error: {Error}");

    public static implicit operator Result<TValue>(TValue value) =>  Success(value);
}