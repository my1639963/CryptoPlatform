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

    // ── V2.0 新增（基于 GM/T 0018-2023 重设计）──

    /// <summary>显式 NotSupported：Provider 可选能力缺失（区别于算法不支持，管理端须明示）</summary>
    public const string PROVIDER_NOT_SUPPORTED = "PROVIDER_NOT_SUPPORTED";

    /// <summary>无私钥使用权限（SDF SDR_PARDENY / SDR_PRKRERR）</summary>
    public const string PROVIDER_PRIVATE_KEY_ACCESS_DENIED = "PROVIDER_PRIVATE_KEY_ACCESS_DENIED";

    /// <summary>密码模块自检失败（V2.10 §3.1.6，自检失败拒绝所有密码运算）</summary>
    public const string PROVIDER_SELF_TEST_FAILED = "PROVIDER_SELF_TEST_FAILED";

    /// <summary>验证签名失败（SDF SDR_VERIFYERR；GCM Tag 失败在 API 层细分 30002）</summary>
    public const string PROVIDER_VERIFY_FAILED = "PROVIDER_VERIFY_FAILED";

    /// <summary>MAC 运算失败（SDF SDR_MACERR）</summary>
    public const string PROVIDER_MAC_FAILED = "PROVIDER_MAC_FAILED";

    /// <summary>输入/输出参数错误（SDF SDR_INARGERR / SDR_OUTARGERR）</summary>
    public const string PROVIDER_BAD_ARGUMENT = "PROVIDER_BAD_ARGUMENT";

    /// <summary>多步运算步骤错误（SDF SDR_STEPERR）</summary>
    public const string PROVIDER_STEP_ERROR = "PROVIDER_STEP_ERROR";

    /// <summary>存储空间不足（SDF SDR_NOBUFFER）</summary>
    public const string PROVIDER_NO_BUFFER = "PROVIDER_NO_BUFFER";
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
