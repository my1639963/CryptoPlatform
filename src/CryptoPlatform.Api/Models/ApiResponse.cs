namespace CryptoPlatform.Api.Models;

/// <summary>
/// 统一响应格式。code 为 int 类型，0=成功，非0=错误码。
/// </summary>
public sealed class ApiResponse<T>
{
    public int Code { get; init; }
    public string Message { get; init; } = "success";
    public T? Data { get; init; }
    public string RequestId { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;

    public static ApiResponse<T> Ok(T data, string requestId) => new()
    {
        Code = 0,
        Data = data,
        RequestId = requestId,
        Timestamp = DateTimeOffset.UtcNow.ToString("o")
    };

    public static ApiResponse<T> Fail(int code, string message, string requestId) => new()
    {
        Code = code,
        Message = message,
        RequestId = requestId,
        Timestamp = DateTimeOffset.UtcNow.ToString("o")
    };
}

/// <summary>
/// 无数据的响应
/// </summary>
public sealed class ApiResponse
{
    public int Code { get; init; }
    public string Message { get; init; } = "success";
    public string RequestId { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;

    public static ApiResponse Ok(string requestId) => new()
    {
        Code = 0,
        RequestId = requestId,
        Timestamp = DateTimeOffset.UtcNow.ToString("o")
    };

    public static ApiResponse Fail(int code, string message, string requestId) => new()
    {
        Code = code,
        Message = message,
        RequestId = requestId,
        Timestamp = DateTimeOffset.UtcNow.ToString("o")
    };
}

/// <summary>
/// 错误响应格式（用于异常中间件）
/// </summary>
public sealed class ApiErrorResponse
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public string RequestId { get; init; } = "";
    public string Timestamp { get; init; } = "";
}
