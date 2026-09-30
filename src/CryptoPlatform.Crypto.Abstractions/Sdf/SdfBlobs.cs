namespace CryptoPlatform.Crypto.Abstractions.Sdf;

/// <summary>
/// 设备信息（GM/T 0018-2023 §5.3 表 2 / DEVICEINFO 结构）。
/// 是平台 ProviderCapabilities 算法能力声明的唯一事实来源（实现规范 §36.4.3）。
/// </summary>
public sealed record SdfDeviceInfo
{
    /// <summary>设备生产厂商名称（≤40 字符）</summary>
    public string IssuerName { get; init; } = "";

    /// <summary>设备型号（≤16 字符）</summary>
    public string DeviceName { get; init; } = "";

    /// <summary>设备编号（≤16 字符）</summary>
    public string DeviceSerial { get; init; } = "";

    /// <summary>密码设备内部软件的版本号</summary>
    public uint DeviceVersion { get; init; }

    /// <summary>密码设备支持的接口规范版本号；GM/T 0018-2023 应为 2</summary>
    public uint StandardVersion { get; init; }

    /// <summary>
    /// 非对称算法能力，2 个 ULONG：
    /// [0] = 支持的非对称算法标识按位或；[1] = 支持的最大模长按位或。
    /// </summary>
    public uint[] AsymAlgAbility { get; init; } = [0, 0];

    /// <summary>所有支持的对称算法标识按位或</summary>
    public uint SymAlgAbility { get; init; }

    /// <summary>所有支持的杂凑算法标识按位或</summary>
    public uint HashAlgAbility { get; init; }

    /// <summary>支持的最大文件存储空间（字节）</summary>
    public uint BufferSize { get; init; }
}

/// <summary>SDF 设备绑定方式（部署模式互斥：Software 或 Native 二选一）。</summary>
public sealed record SdfDeviceBinding
{
    /// <summary>绑定类型："SOFT" / "NATIVE"</summary>
    public string BindingType { get; init; } = "";

    /// <summary>厂商（IssuerName 或登记值）</summary>
    public string? Vendor { get; init; }

    /// <summary>厂商 SDF 动态库路径（NATIVE 时填写；平台不依赖 Docker，按部署环境绝对路径加载）</summary>
    public string? LibraryPath { get; init; }

    /// <summary>登记的商用密码产品认证证书编号（仅 HSM，F-DEV-008）</summary>
    public string? CertificationNo { get; init; }
}

/// <summary>SDF 健康探测结果。</summary>
public sealed record SdfHealth
{
    public bool Healthy { get; init; }

    /// <summary>状态描述</summary>
    public string? Message { get; init; }

    /// <summary>探测耗时（OpenSession → GetDeviceInfo → CloseSession 往返）</summary>
    public TimeSpan Latency { get; init; }
}

/* ─────────────────────────────────────────────────────────────
 * 数据结构托管映射（GM/T 0018-2023 §5.5～§5.9）。
 *
 * 托管侧统一保存“变宽有效值”，64 字节定宽大端补零的编解码由设备绑定层完成
 * （实现规范-HSM适配器 §37.4 编解码强制规则）。
 * ───────────────────────────────────────────────────────────── */

/// <summary>ECC 公钥（§5.6 ECCrefPublicKey：bits + x[64] + y[64]，大端定宽）。</summary>
public sealed record SdfEccPublicKey
{
    /// <summary>密钥位长（SM2 为 256）</summary>
    public int Bits { get; init; }

    /// <summary>公钥 x 坐标（有效长度 = (Bits+7)/8）</summary>
    public required byte[] X { get; init; }

    /// <summary>公钥 y 坐标（有效长度 = (Bits+7)/8）</summary>
    public required byte[] Y { get; init; }

    /// <summary>未压缩编码 04||X||Y（平台对外形态）</summary>
    public byte[] ToUncompressedPoint()
    {
        var result = new byte[1 + X.Length + Y.Length];
        result[0] = 0x04;
        Buffer.BlockCopy(X, 0, result, 1, X.Length);
        Buffer.BlockCopy(Y, 0, result, 1 + X.Length, Y.Length);
        return result;
    }
}

/// <summary>ECC 私钥（§5.6 ECCrefPrivateKey：bits + K[64]，大端定宽）。仅存在于设备内或测试路径，禁止入库。</summary>
public sealed record SdfEccPrivateKey
{
    public int Bits { get; init; }

    /// <summary>私钥有效值（有效长度 = (Bits+7)/8）</summary>
    public required byte[] K { get; init; }
}

/// <summary>ECC 签名（§5.8 ECCSignature：r[64] + s[64]，定宽大端）。
/// 与平台默认 DER 编码的互转由 Provider 层负责。</summary>
public sealed record SdfEccSignature
{
    public required byte[] R { get; init; }

    public required byte[] S { get; init; }
}

/// <summary>
/// ECC 加密数据（§5.7 ECCCipher：x[64] + y[64] + M[32] + L + C[L]）。
/// 结构序即 C1||C3||C2（与需求默认密文编码一致，但本结构为定宽大端，
/// 与平台 REST 层的变宽 C1C3C2 互转在 Provider 层完成）。
/// </summary>
public sealed record SdfEccCipher
{
    /// <summary>密文椭圆曲线点 x 分量（C1 前半）</summary>
    public required byte[] X { get; init; }

    /// <summary>密文椭圆曲线点 y 分量（C1 后半）</summary>
    public required byte[] Y { get; init; }

    /// <summary>明文的杂凑值（C3，32 字节）</summary>
    public required byte[] Hash { get; init; }

    /// <summary>密文数据（C2，长度必须等于原结构中的 L）</summary>
    public required byte[] Cipher { get; init; }
}

/// <summary>
/// ECC 加密密钥对保护结构（§5.9 EnvelopedECCKey）。
/// 用于密钥管理系统向设备下发 ECC 加密密钥对；ulSymmAlgID 必须为 ECB 模式。
/// </summary>
public sealed record SdfEnvelopedEccKey
{
    /// <summary>版本号，本版本为 1</summary>
    public uint Version { get; init; } = 1;

    /// <summary>对称算法标识（必须为 ECB 模式）</summary>
    public SdfAlgorithmId SymmAlgId { get; init; }

    /// <summary>ECC 密钥对的密钥位长</summary>
    public uint Bits { get; init; }

    /// <summary>对称算法加密的 ECC 私钥密文（原文为 ECCrefPrivateKey.K）</summary>
    public required byte[] EncryptedPrivKey { get; init; }

    /// <summary>ECC 密钥对的公钥</summary>
    public required SdfEccPublicKey PubKey { get; init; }

    /// <summary>用保护公钥加密过的对称密钥密文</summary>
    public required SdfEccCipher KeyCipher { get; init; }
}
