namespace PriceTracker.Api.Models.Core;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; set; }
    public Error? ErrorInfo { get; set; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        ErrorInfo = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Value = default;
        ErrorInfo = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}