using System.Text.Json.Serialization;

namespace IdentityUoW.Api.Common.Models;

public abstract class ApiResult
{
    public bool IsSucceeded { get; init; }

    public string? Message { get; init; }

    /// <summary>HTTP status the controller should return. Not serialized.</summary>
    [JsonIgnore]
    public int StatusCode { get; init; } = StatusCodes.Status200OK;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}

public sealed class ApiSuccessResult<T> : ApiResult<T>
{
    public ApiSuccessResult(T data, string message = "Success")
    {
        IsSucceeded = true;
        Data = data;
        Message = message;
    }
}

public sealed class ApiErrorResult<T> : ApiResult<T>
{
    public ApiErrorResult(string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        IsSucceeded = false;
        Message = message;
        StatusCode = statusCode;
    }

    public ApiErrorResult(IEnumerable<string> errors, int statusCode = StatusCodes.Status400BadRequest)
        : this("One or more errors occurred.", statusCode)
    {
        Errors = [.. errors];
    }

    public List<string> Errors { get; init; } = [];
}
