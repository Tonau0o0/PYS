namespace PYS.Service.Common;

public enum ResultStatus
{
    Success,
    NotFound,
    ValidationError,
    Conflict,
    Unauthorized,
    Failure
}

public class ServiceResult
{
    public bool IsSuccess => Status == ResultStatus.Success;
    public ResultStatus Status { get; }
    public string? Error { get; }
    public IReadOnlyList<string> ValidationErrors { get; }

    protected ServiceResult(ResultStatus status, string? error = null, IReadOnlyList<string>? validationErrors = null)
    {
        Status = status;
        Error = error;
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }

    public static ServiceResult Success() => new(ResultStatus.Success);
    public static ServiceResult NotFound(string message) => new(ResultStatus.NotFound, message);
    public static ServiceResult ValidationFailed(IReadOnlyList<string> errors) => new(ResultStatus.ValidationError, "Validation failed", errors);
    public static ServiceResult Conflict(string message) => new(ResultStatus.Conflict, message);
    public static ServiceResult Failure(string message) => new(ResultStatus.Failure, message);
}

public sealed class ServiceResult<T> : ServiceResult
{
    public T? Data { get; }

    private ServiceResult(T data) : base(ResultStatus.Success) => Data = data;
    private ServiceResult(ResultStatus status, string? error, IReadOnlyList<string>? errors)
        : base(status, error, errors) { }

    public static ServiceResult<T> Success(T data) => new(data);
    public static new ServiceResult<T> NotFound(string message) => new(ResultStatus.NotFound, message, null);
    public static new ServiceResult<T> ValidationFailed(IReadOnlyList<string> errors) => new(ResultStatus.ValidationError, "Validation failed", errors);
    public static new ServiceResult<T> Conflict(string message) => new(ResultStatus.Conflict, message, null);
    public static new ServiceResult<T> Failure(string message) => new(ResultStatus.Failure, message, null);
}
