namespace CryptoPlatform.Crypto.Abstractions.Sdf;

/// <summary>
/// GM/T 0018-2023 密码设备的托管映射（设备级）。
/// 对应 SDF_OpenDevice / SDF_CloseDevice / SDF_OpenSession / SDF_CloseSession / SDF_GetDeviceInfo。
/// <para>实现必须线程安全；会话对象（<see cref="ISdfSession"/>）非线程安全，由会话池借还。</para>
/// </summary>
public interface ISdfDevice : IAsyncDisposable
{
    /// <summary>设备唯一标识（对应 sys_crypto_device.DeviceId）</summary>
    string DeviceId { get; }

    /// <summary>设备绑定方式（Soft / Native）</summary>
    SdfDeviceBinding Binding { get; }

    /// <summary>是否已打开（SDF_OpenDevice 成功且未 Close）</summary>
    bool IsOpen { get; }

    /// <summary>打开设备（SDF_OpenDevice）。重复打开应幂等或抛出 SdfException(SDR_OPENDEVICE)。</summary>
    Task OpenAsync(CancellationToken ct = default);

    /// <summary>创建会话（SDF_OpenSession）。关闭由调用方/会话池负责（SDF_CloseSession）。</summary>
    ISdfSession OpenSession();

    /// <summary>获取设备能力描述（SDF_GetDeviceInfo）。能力探测的唯一入口。</summary>
    Task<SdfDeviceInfo> GetDeviceInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// 健康探测：OpenSession → GetDeviceInfo → CloseSession 往返计时。
    /// </summary>
    Task<SdfHealth> CheckHealthAsync(CancellationToken ct = default);
}

/// <summary>会话密钥句柄（GM/T 0018-2023 §5.4.3：会话密钥使用句柄检索）。运行期对象，不得持久化。</summary>
public interface ISdfKeyHandle : IDisposable
{
    /// <summary>密钥位长</summary>
    int KeyBits { get; }
}

/// <summary>会话密钥生成/导入结果。WrappedKey 为 KEK/IPK/EPK 保护形态，即平台 WrappedMaterial。</summary>
public sealed record SdfSessionKey(ISdfKeyHandle Handle, byte[]? WrappedKey, int KeyBits);

/// <summary>密钥协商参数输出（SDF_GenerateAgreementDataWithECC）。</summary>
public sealed record SdfAgreementData(ISdfKeyHandle? Handle, SdfEccPublicKey TemporaryPublicKey, byte[] AgreementId);

/// <summary>可鉴别加密（CCM/GCM）单包结果（SDF_AuthEnc）。</summary>
public sealed record SdfAuthResult(byte[] Ciphertext, byte[] AuthTag);

/// <summary>三步式杂凑（SDF_HashInit/Update/Final、SDF_HMACInit/Update/Final）的状态对象。</summary>
public interface ISdfHash : IDisposable
{
    /// <summary>第二步：多包杂凑运算（SDF_HashUpdate / SDF_HMACUpdate）</summary>
    void Update(ReadOnlySpan<byte> data);

    /// <summary>第三步：结束并返回杂凑值，清除中间状态（SDF_HashFinal / SDF_HMACFinal）</summary>
    byte[] Final();
}

/// <summary>多包对称加密 / 解密状态（SDF_EncryptInit/Update/Final、SDF_DecryptInit/Update/Final）。
/// 生命周期绑定其所属会话，禁止跨会话使用；异常路径必须调用 Dispose 释放设备资源。</summary>
public interface ISdfMultiPacket : IDisposable
{
    /// <summary>多包运算（SDF_EncryptUpdate / SDF_DecryptUpdate）。设备侧不做填充。</summary>
    byte[] Update(ReadOnlySpan<byte> data);

    /// <summary>结束并释放资源（SDF_EncryptFinal / SDF_DecryptFinal）。最后一段输出可能为空。</summary>
    byte[] Final();
}

/// <summary>多包 MAC 状态（SDF_CalculateMACInit/Update/Final）。设备侧不做填充。</summary>
public interface ISdfMacMultiPacket : IDisposable
{
    /// <summary>多包 MAC 计算（SDF_CalculateMACUpdate）</summary>
    void Update(ReadOnlySpan<byte> data);

    /// <summary>返回 MAC 结果并释放资源（SDF_CalculateMACFinal）</summary>
    byte[] Final();
}

/// <summary>多包可鉴别加密状态（SDF_AuthEncInit/Update/Final，CCM/GCM）。</summary>
public interface ISdfAuthEncMultiPacket : IDisposable
{
    /// <summary>多包可鉴别加密（SDF_AuthEncUpdate）</summary>
    byte[] Update(ReadOnlySpan<byte> data);

    /// <summary>结束（SDF_AuthEncFinal）：输出最后一段密文；鉴别数据经 <see cref="AuthTag"/> 读取。</summary>
    byte[] Final();

    /// <summary>鉴别数据（Tag）。Final 成功后可读。</summary>
    byte[]? AuthTag { get; }
}

/// <summary>多包可鉴别解密状态（SDF_AuthDecInit/Update/Final，CCM/GCM）。Tag 校验失败抛 SdfException(SDR_VERIFYERR)。</summary>
public interface ISdfAuthDecMultiPacket : IDisposable
{
    /// <summary>多包可鉴别解密（SDF_AuthDecUpdate）</summary>
    byte[] Update(ReadOnlySpan<byte> data);

    /// <summary>结束并校验鉴别数据（SDF_AuthDecFinal）：输出最后一段明文。</summary>
    byte[] Final(byte[] authTag);
}
