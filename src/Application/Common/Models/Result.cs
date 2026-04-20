namespace POS.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;

    public Result() { }

    public Result(bool success, string message)
    {
        IsSuccess = success;
        ErrorMessage = message;
    }

    public static Result Success() => new();
    public static Result Failure(string message) => new(false, message);
}

public class Result<T> : Result
{
    public T? Data { get; set; }

    public Result() { }

    public Result(bool success, string message) : base(success, message) { }

    public Result(T data)
    {
        IsSuccess = true;
        Data = data;
    }

    public static Result<T> Success(T data) => new(data);
    public static new Result<T> Failure(string message) => new(false, message);
}
