namespace CoEdit.Common.Domain.Shared;

public class Result
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>();

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    protected Result(bool isSuccess, string error, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var normalizedErrors = errors is { Count: > 0 } ? errors : EmptyErrors;

        if (isSuccess && (error != string.Empty || normalizedErrors.Count > 0))
            throw new InvalidOperationException();
        if (!isSuccess && error == string.Empty && normalizedErrors.Count == 0)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
        Errors = normalizedErrors;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(string error, IReadOnlyDictionary<string, string[]> errors) => new(false, error, errors);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(string error, IReadOnlyDictionary<string, string[]> errors) =>
        Result<T>.Failure(error, errors);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("The value of a failure result can not be accessed.");
            return _value!;
        }
    }

    protected Result(
        T? value,
        bool isSuccess,
        string error,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(isSuccess, error, errors)
    {
        _value = value;
    }

    public static Result<T> Success(T value) => new(value, true, string.Empty);
    public new static Result<T> Failure(string error) => new(default, false, error);
    public new static Result<T> Failure(string error, IReadOnlyDictionary<string, string[]> errors) =>
        new(default, false, error, errors);
}