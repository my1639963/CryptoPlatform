namespace CryptoPlatform.Crypto.Abstractions.Sdf;

/// <summary>
/// GM/T 0018-2023 算法标识透传值。
/// <para>
/// 数值的唯一权威来源是 GM/T 0006《密码应用标识规范》（GM/T 0018-2023 §5.1）；
/// 本抽象层不做任何数值定义，由设备绑定层（SoftSdfDevice / NativeSdfDevice）负责
/// 平台算法枚举 → 设备算法标识的映射。在 GM/T 0006 数值冻结前，
/// 任何在本层硬编码 SGD_* 数值的行为都视为违规（见实现规范-HSM适配器 §37.13 待确认-2）。
/// </para>
/// </summary>
public readonly record struct SdfAlgorithmId(uint Value)
{
    /// <inheritdoc />
    public override string ToString() => $"SGD(0x{Value:X8})";
}

/// <summary>
/// GM/T 0018-2023 附录 A（规范性）函数返回代码。
/// SDR_OK = 0x00000000；SDR_BASE = 0x01000000。
/// </summary>
public static class SdfErrorCode
{
    /// <summary>操作成功</summary>
    public const int SDR_OK = 0x00000000;

    /// <summary>错误码基础值</summary>
    public const int SDR_BASE = unchecked((int)0x01000000);

    /// <summary>未知错误（SDR_BASE + 0x01）</summary>
    public const int SDR_UNKNOWERR = SDR_BASE + 0x01;

    /// <summary>不支持的接口调用（SDR_BASE + 0x02）</summary>
    public const int SDR_NOTSUPPORT = SDR_BASE + 0x02;

    /// <summary>与设备通信失败（SDR_BASE + 0x03）</summary>
    public const int SDR_COMMFAIL = SDR_BASE + 0x03;

    /// <summary>运算模块无响应（SDR_BASE + 0x04）</summary>
    public const int SDR_HARDFAIL = SDR_BASE + 0x04;

    /// <summary>打开设备失败（SDR_BASE + 0x05）</summary>
    public const int SDR_OPENDEVICE = SDR_BASE + 0x05;

    /// <summary>创建会话失败（SDR_BASE + 0x06）</summary>
    public const int SDR_OPENSESSION = SDR_BASE + 0x06;

    /// <summary>无私钥使用权限（SDR_BASE + 0x07）</summary>
    public const int SDR_PARDENY = SDR_BASE + 0x07;

    /// <summary>不存在的密钥调用（SDR_BASE + 0x08）</summary>
    public const int SDR_KEYNOTEXIST = SDR_BASE + 0x08;

    /// <summary>不支持的算法调用（SDR_BASE + 0x09）</summary>
    public const int SDR_ALGNOTSUPPORT = SDR_BASE + 0x09;

    /// <summary>不支持的算法模式调用（SDR_BASE + 0x0A）</summary>
    public const int SDR_ALGMODNOTSUPPORT = SDR_BASE + 0x0A;

    /// <summary>公钥运算失败（SDR_BASE + 0x0B）</summary>
    public const int SDR_PKOPERR = SDR_BASE + 0x0B;

    /// <summary>私钥运算失败（SDR_BASE + 0x0C）</summary>
    public const int SDR_SKOPERR = SDR_BASE + 0x0C;

    /// <summary>签名运算失败（SDR_BASE + 0x0D）</summary>
    public const int SDR_SIGNERR = SDR_BASE + 0x0D;

    /// <summary>验证签名失败（SDR_BASE + 0x0E）</summary>
    public const int SDR_VERIFYERR = SDR_BASE + 0x0E;

    /// <summary>对称算法运算失败（SDR_BASE + 0x0F）</summary>
    public const int SDR_SYMOPERR = SDR_BASE + 0x0F;

    /// <summary>多步运算步骤错误（SDR_BASE + 0x10）</summary>
    public const int SDR_STEPERR = SDR_BASE + 0x10;

    /// <summary>文件长度超出限制（SDR_BASE + 0x11）</summary>
    public const int SDR_FILESIZEERR = SDR_BASE + 0x11;

    /// <summary>指定的文件不存在（SDR_BASE + 0x12）</summary>
    public const int SDR_FILENOEXIST = SDR_BASE + 0x12;

    /// <summary>文件起始位置错误（SDR_BASE + 0x13）</summary>
    public const int SDR_FILEOFSERR = SDR_BASE + 0x13;

    /// <summary>密钥类型错误（SDR_BASE + 0x14）</summary>
    public const int SDR_KEYTYPEERR = SDR_BASE + 0x14;

    /// <summary>密钥错误（SDR_BASE + 0x15）</summary>
    public const int SDR_KEYERR = SDR_BASE + 0x15;

    /// <summary>ECC 加密数据错误（SDR_BASE + 0x16）</summary>
    public const int SDR_ENCDATAERR = SDR_BASE + 0x16;

    /// <summary>随机数产生失败（SDR_BASE + 0x17）</summary>
    public const int SDR_RANDERR = SDR_BASE + 0x17;

    /// <summary>私钥使用权限获取失败（SDR_BASE + 0x18）</summary>
    public const int SDR_PRKRERR = SDR_BASE + 0x18;

    /// <summary>MAC 运算失败（SDR_BASE + 0x19）</summary>
    public const int SDR_MACERR = SDR_BASE + 0x19;

    /// <summary>指定文件已存在（SDR_BASE + 0x1A）</summary>
    public const int SDR_FILEEXISTS = SDR_BASE + 0x1A;

    /// <summary>文件写入失败（SDR_BASE + 0x1B）</summary>
    public const int SDR_FILEWERR = SDR_BASE + 0x1B;

    /// <summary>存储空间不足（SDR_BASE + 0x1C）</summary>
    public const int SDR_NOBUFFER = SDR_BASE + 0x1C;

    /// <summary>输入参数错误（SDR_BASE + 0x1D）</summary>
    public const int SDR_INARGERR = SDR_BASE + 0x1D;

    /// <summary>输出参数错误（SDR_BASE + 0x1E）</summary>
    public const int SDR_OUTARGERR = SDR_BASE + 0x1E;

    /// <summary>用户标识错误（SDR_BASE + 0x1F，GM/T 0018-2023 新增）</summary>
    public const int SDR_USERIDERR = SDR_BASE + 0x1F;
}

/// <summary>
/// SDF 密钥载体类型（GM/T 0018-2023 §5.4 密钥分类及存储定义）。
/// </summary>
public enum SdfKeyKind
{
    /// <summary>密钥对索引（§5.4.1）：非对称密钥对，索引 0 为设备密钥，1 起为用户密钥；每个索引含一个签名密钥对与一个加密密钥对</summary>
    KeyIndex,

    /// <summary>密钥加密密钥 KEK 索引（§5.4.2）：索引号从 1 开始</summary>
    KekIndex,

    /// <summary>会话密钥（§5.4.3）：由接口函数生成或导入，以句柄检索；运行期对象，不得持久化</summary>
    Session,
}

/// <summary>
/// Provider 内部密钥引用（稳定标识）。
/// 平台持久化的是本引用；SDF 运行期句柄（IntPtr / ISdfKeyHandle）不得入库。
/// 字符串形态：{ProviderId}:{Kind}:{Identity}。
/// </summary>
public sealed record SdfKeyReference(string ProviderId, SdfKeyKind Kind, string Identity)
{
    /// <inheritdoc />
    public override string ToString() => $"{ProviderId}:{Kind}:{Identity}";
}

/// <summary>
/// SDF 调用异常。携带 GM/T 0018-2023 附录 A 原始错误码与产生错误的函数名。
/// 原始错误码仅允许进入日志与审计明细，不得外泄到 API 响应（实现规范-HSM适配器 §37.7）。
/// </summary>
public sealed class SdfException : Exception
{
    /// <summary>附录 A 原始错误码</summary>
    public int ErrorCode { get; }

    /// <summary>产生错误的 SDF 函数名（如 "SDF_Encrypt"）</summary>
    public string? FunctionName { get; }

    /// <summary>
    /// 会话是否仍健康。为 false 时（COMMFAIL / HARDFAIL 等）会话池必须销毁该会话而非回池。
    /// </summary>
    public bool SessionHealthy { get; }

    public SdfException(int errorCode, string? functionName, string message, bool sessionHealthy = true)
        : base($"[{functionName}] 0x{errorCode:X8}: {message}")
    {
        ErrorCode = errorCode;
        FunctionName = functionName;
        SessionHealthy = sessionHealthy;
    }
}
