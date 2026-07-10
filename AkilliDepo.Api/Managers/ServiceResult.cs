namespace AkilliDepo.Api.Managers;

public enum ServiceStatus
{
    Ok,
    BadRequest,
    Forbidden,
    NotFound
}

public class ServiceResult<T>
{
    public ServiceStatus Status { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public bool IsSuccess => Status == ServiceStatus.Ok;

    public static ServiceResult<T> Ok(T data, string? message = null) =>
        new() { Status = ServiceStatus.Ok, Data = data, Message = message };

    public static ServiceResult<T> BadRequest(string message) =>
        new() { Status = ServiceStatus.BadRequest, Message = message };

    public static ServiceResult<T> Forbidden(string message) =>
        new() { Status = ServiceStatus.Forbidden, Message = message };

    public static ServiceResult<T> NotFound(string message) =>
        new() { Status = ServiceStatus.NotFound, Message = message };
}
