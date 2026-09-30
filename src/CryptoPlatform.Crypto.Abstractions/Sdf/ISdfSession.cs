namespace CryptoPlatform.Crypto.Abstractions.Sdf;

/// <summary>
/// GM/T 0018-2023 会话级生产函数（§6.2～§6.7）的托管映射。
/// <para>
/// 覆盖范围：6.2 设备管理（会话级 4 个）、6.3 密钥管理（ECC/KEK/销毁 11 个，RSA 5 个不纳入契约）、
/// 6.4 非对称运算（ECC 4 个，RSA 3 个不纳入契约）、6.5 对称运算 20 个、6.6 杂凑运算 6 个、
/// 6.7 用户文件 4 个（默认不启用）。
/// <b>第 6.8 节验证调试类函数（12 个）不得进入本契约</b>（实现规范-HSM适配器 §37.11）。
/// </para>
/// <para>
/// 约定：所有方法失败时抛出 <see cref="SdfException"/>；RSA 族函数可抛出
/// SdfException(SDR_NOTSUPPORT)；实现负责与设备之间的大端（Big-Endian）与定宽结构编解码。
/// </para>
/// </summary>
public interface ISdfSession : IDisposable
{
    // ════════════════ 6.2 设备管理类（会话级） ════════════════

    /// <summary>SDF_GetDeviceInfo：获取密码设备能力描述</summary>
    SdfDeviceInfo GetDeviceInfo();

    /// <summary>SDF_GenerateRandom：获取指定长度随机数</summary>
    byte[] GenerateRandom(int length);

    /// <summary>
    /// SDF_GetPrivateKeyAccessRight：获取指定索引私钥的使用权。
    /// 索引起始值 1（0 为设备密钥）；访问控制码不少于 8 字节（规范 §6.2.8）。
    /// </summary>
    void GetPrivateKeyAccessRight(int keyIndex, string password);

    /// <summary>SDF_ReleasePrivateKeyAccessRight：释放指定索引私钥的使用授权</summary>
    void ReleasePrivateKeyAccessRight(int keyIndex);

    // ════════════════ 6.3 密钥管理类（ECC/KEK，共 11 个；RSA 5 个不纳入契约） ════════════════

    /// <summary>SDF_ExportSignPublicKey_ECC：导出指定索引的 ECC（SM2）签名公钥</summary>
    SdfEccPublicKey ExportSignPublicKeyEcc(int keyIndex);

    /// <summary>SDF_ExportEncPublicKey_ECC：导出指定索引的 ECC（SM2）加密公钥</summary>
    SdfEccPublicKey ExportEncPublicKeyEcc(int keyIndex);

    /// <summary>
    /// SDF_GenerateKeyWithIPK_ECC：生成会话密钥并用内部 ECC 加密公钥加密输出，同时返回句柄。
    /// 返回的 WrappedKey 即平台 L2→L3 语义下的密钥下发密文。
    /// </summary>
    SdfSessionKey GenerateKeyWithIpkEcc(int ipkIndex, int keyBits);

    /// <summary>SDF_GenerateKeyWithEPK_ECC：生成会话密钥并用外部 ECC 公钥加密输出</summary>
    SdfSessionKey GenerateKeyWithEpkEcc(int keyBits, SdfEccPublicKey publicKey);

    /// <summary>SDF_ImportKeyWithISK_ECC：导入会话密钥并用内部 ECC 加密私钥解密，返回句柄</summary>
    SdfSessionKey ImportKeyWithIskEcc(int iskIndex, ReadOnlySpan<byte> wrappedKey);

    /// <summary>
    /// SDF_GenerateKeyWithKEK：生成会话密钥并用指定索引的 KEK 加密输出。
    /// 平台 Data Key 生成与 KEK 往返准入校验（wrap→unwrap）的标准路径。
    /// </summary>
    SdfSessionKey GenerateKeyWithKek(int keyBits, SdfAlgorithmId algId, int kekIndex);

    /// <summary>SDF_ImportKeyWithKEK：导入 KEK 保护的会话密钥（加密模式为 ECB，规范 §6.3.16）</summary>
    SdfSessionKey ImportKeyWithKek(SdfAlgorithmId algId, int kekIndex, ReadOnlySpan<byte> wrappedKey);

    /// <summary>SDF_DestroyKey：销毁会话密钥并释放句柄资源；对称运算结束后必须调用</summary>
    void DestroyKey(ISdfKeyHandle keyHandle);

    // 以下密钥协商三函数：平台 V1.0 不启用；托管形态为骨架，启用前须按规范原文细化并补测试。

    /// <summary>SDF_GenerateAgreementDataWithECC：生成密钥协商参数并输出（V1.0 不启用）</summary>
    SdfAgreementData GenerateAgreementDataWithEcc(int iskIndex, int keyBits, string userId);

    /// <summary>SDF_GenerateKeyWithECC：计算会话密钥（V1.0 不启用）</summary>
    SdfSessionKey GenerateKeyWithEcc(SdfAgreementData selfData, SdfEccPublicKey peerPublicKey, SdfAgreementData peerData, string userId);

    /// <summary>SDF_GenerateAgreementDataAndKeyWithECC：产生协商数据并计算会话密钥（V1.0 不启用）</summary>
    SdfSessionKey GenerateAgreementDataAndKeyWithEcc(int iskIndex, int keyBits, string userId, SdfEccPublicKey peerPublicKey);

    // ════════════════ 6.4 非对称算法运算类（ECC 4 个；RSA 3 个不纳入契约） ════════════════

    /// <summary>
    /// SDF_InternalSign_ECC：使用内部指定索引私钥做 ECC 签名。
    /// 输入必须为待签数据的杂凑值（SM2 时为 GB/T 35276 签名预处理结果，
    /// 可经 <see cref="HashInit"/> 的 SM2 预处理1 产生）。
    /// 调用前须完成 GetPrivateKeyAccessRight 授权。
    /// </summary>
    SdfEccSignature InternalSignEcc(int iskIndex, ReadOnlySpan<byte> hashedData);

    /// <summary>SDF_InternalVerify_ECC：使用内部指定索引公钥验证 ECC 签名（输入为预处理后的杂凑值）</summary>
    bool InternalVerifyEcc(int iskIndex, ReadOnlySpan<byte> hashedData, SdfEccSignature signature);

    /// <summary>SDF_ExternalVerify_ECC：使用外部公钥验证 ECC 签名（输入为预处理后的杂凑值）</summary>
    bool ExternalVerifyEcc(SdfAlgorithmId algId, SdfEccPublicKey publicKey, ReadOnlySpan<byte> hashedData, SdfEccSignature signature);

    /// <summary>SDF_ExternalEncrypt_ECC：使用外部公钥对数据进行 ECC 加密（输出 ECCCipher，结构序 C1C3C2）</summary>
    SdfEccCipher ExternalEncryptEcc(SdfAlgorithmId algId, SdfEccPublicKey publicKey, ReadOnlySpan<byte> data);

    // ════════════════ 6.5 对称算法运算类（20 个） ════════════════
    // 注意：规范 §6.5.2/6.5.3/6.5.4 明确单包加解密与 MAC 均不对数据做填充处理；
    // IV 长度与算法分组长度相同。PKCS#7 填充属 Provider 层职责。

    /// <summary>SDF_Encrypt：单包对称加密（不填充）</summary>
    byte[] Encrypt(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> data);

    /// <summary>SDF_Decrypt：单包对称解密（不填充）</summary>
    byte[] Decrypt(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> data);

    /// <summary>SDF_CalculateMAC：单包 MAC 计算（不填充；MAC 算法标识约定同分组密码算法）</summary>
    byte[] CalculateMac(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> data);

    /// <summary>
    /// SDF_AuthEnc：单包可鉴别加密（CCM/GCM，GB/T 36624）。
    /// SM4-GCM 场景：startVar = 平台确定性 Nonce（12 字节），aad = 附加认证数据，Tag 经返回值携带。
    /// </summary>
    SdfAuthResult AuthEnc(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> startVar, ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext);

    /// <summary>
    /// SDF_AuthDec：单包可鉴别解密（CCM/GCM）。
    /// 鉴别数据校验失败抛出 SdfException(SDR_VERIFYERR) → 平台映射为 30002。
    /// </summary>
    byte[] AuthDec(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> startVar, ReadOnlySpan<byte> aad, ReadOnlySpan<byte> authTag, ReadOnlySpan<byte> ciphertext);

    /// <summary>SDF_EncryptInit：多包对称加密初始化</summary>
    ISdfMultiPacket EncryptInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv);

    /// <summary>SDF_DecryptInit：多包对称解密初始化</summary>
    ISdfMultiPacket DecryptInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv);

    /// <summary>SDF_CalculateMACInit：多包 MAC 初始化</summary>
    ISdfMacMultiPacket CalculateMacInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv);

    /// <summary>
    /// SDF_AuthEncInit：多包可鉴别加密初始化（CCM/GCM）。
    /// totalDataLength 为 CCM 模式的明文总长度，GCM 模式可传 null。
    /// </summary>
    ISdfAuthEncMultiPacket AuthEncInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> startVar, ReadOnlySpan<byte> aad, long? totalDataLength);

    /// <summary>SDF_AuthDecInit：多包可鉴别解密初始化（CCM/GCM）</summary>
    ISdfAuthDecMultiPacket AuthDecInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> startVar, ReadOnlySpan<byte> aad, long? totalDataLength);

    // ════════════════ 6.6 杂凑运算类（6 个） ════════════════

    /// <summary>
    /// SDF_HashInit：三步式杂凑第一步。
    /// userId 非空且 algId 为 SGD_SM3 时执行 SM2 预处理1（Z 值）计算，此时 signerPubKey 不能为空
    /// （规范 §6.6.5，过程符合 GB/T 35276）；纯 SM3 杂凑传 signerPubKey=null、userId 空。
    /// </summary>
    ISdfHash HashInit(SdfAlgorithmId algId, SdfEccPublicKey? signerPubKey, ReadOnlySpan<byte> userId);

    /// <summary>SDF_HMACInit：三步式带密钥杂凑第一步（GB/T 15852.2）</summary>
    ISdfHash HmacInit(ISdfKeyHandle key, SdfAlgorithmId algId);

    // ════════════════ 6.7 用户文件操作类（4 个，平台 V1.0 能力关闭） ════════════════

    /// <summary>SDF_CreateFile：在设备内创建用户数据文件（文件名最大 128 字节）</summary>
    void CreateFile(string fileName, int fileSize);

    /// <summary>SDF_ReadFile：读取设备内文件内容（length 入参为缓冲长度，出参为实际读取长度）</summary>
    byte[] ReadFile(string fileName, int offset, ref int length);

    /// <summary>SDF_WriteFile：向设备内文件写入内容</summary>
    void WriteFile(string fileName, int offset, ReadOnlySpan<byte> data);

    /// <summary>SDF_DeleteFile：删除设备内指定文件</summary>
    void DeleteFile(string fileName);
}
