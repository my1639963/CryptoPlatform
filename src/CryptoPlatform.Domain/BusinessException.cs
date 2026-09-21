namespace CryptoPlatform.Domain;

/// <summary>
/// 业务异常基类。携带错误码，用于全局异常处理中间件统一格式化。
/// </summary>
public class BusinessException : Exception
{
    public string Code { get; }

    public BusinessException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public BusinessException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
