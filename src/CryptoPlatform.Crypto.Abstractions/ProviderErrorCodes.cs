namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// Provider 统一错误码。
/// 内部保留厂商原始错误码，外部只返回此统一错误码。
/// </summary>
public static class ProviderErrorCodes
{
    /// <summary>密码设备/模块不可用</summary>
    public const string PROVIDER_UNAVAILABLE = "PROVIDER_UNAVAILABLE";

    /// <summary>调用超时</summary>
    public const string PROVIDER_TIMEOUT = "PROVIDER_TIMEOUT";

    /// <summary>Provider 中密钥不存在</summary>
    public const string PROVIDER_KEY_NOT_FOUND = "PROVIDER_KEY_NOT_FOUND";

    /// <summary>Provider 中密钥无效</summary>
    public const string PROVIDER_KEY_INVALID = "PROVIDER_KEY_INVALID";

    /// <summary>操作执行失败</summary>
    public const string PROVIDER_OPERATION_FAILED = "PROVIDER_OPERATION_FAILED";

    /// <summary>设备离线</summary>
    public const string PROVIDER_DEVICE_OFFLINE = "PROVIDER_DEVICE_OFFLINE";

    /// <summary>Provider 认证失败</summary>
    public const string PROVIDER_AUTH_FAILED = "PROVIDER_AUTH_FAILED";

    /// <summary>能力不支持</summary>
    public const string PROVIDER_CAPABILITY_NOT_SUPPORTED = "PROVIDER_CAPABILITY_NOT_SUPPORTED";
}

/// <summary>
/// Provider 专用异常类。携带统一错误码。
/// </summary>
public class CryptoProviderException : Exception
{
    /// <summary>统一错误码</summary>
    public string ErrorCode { get; }

    public CryptoProviderException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public CryptoProviderException(string errorCode, string message, Exception inner)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
