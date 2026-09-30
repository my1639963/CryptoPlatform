# 37. 密码设备适配层规范（GM/T 0018-2023 SDF 映射）

| 项目     | 内容                                                            |
| -------- | --------------------------------------------------------------- |
| 文档名称 | 密码设备适配层（SDF 映射）实现规范                              |
| 文档版本 | V2.0                                                            |
| 编写日期 | 2026-09-30                                                      |
| 文档状态 | 已评审待定稿                                                    |
| 上一版本 | V1.0（`IHsmSdkAdapter` 自定义厂商防腐层，无标准依据）           |
| 文档用途 | 约束 `ISdfDevice`/`ISdfSession`、`SoftSdfDevice`、`NativeSdfDevice` 的设计与实现 |
| 合规基线 | GB/T 22239-2019（等保三级）、GB/T 39786-2021（密评三级）、信创  |
| 规范基线 | **GM/T 0018-2023《密码设备应用接口规范》**（2023-12-04 发布，2024-06-01 实施，代替 GM/T 0018-2012） |
| 需求基线 | 《国密加密服务系统软件需求规格说明书》V2.10 §2.6、§3.7、§5.6、§8.10 |

> **本版修订说明（V2.0）**：V1.0 的 `IHsmSdkAdapter` 是自定义厂商防腐层，接口形态（连接池 + 字符串算法名 + 不透明句柄）与任何标准无关，导致：① 每接一家厂商就要重写一遍适配语义；② 无法用标准条款与厂商对齐验收；③ 软件密码模块只能"另写一套"，与 HSM 行为一致性无法论证。本版以 GM/T 0018-2023 为唯一设备契约重建适配层。

---

## 37.1 规范事实基线（实现前必读）

> 以下全部结论逐条对照 GM/T 0018-2023 原文核证（86 页扫描件全文 OCR + 关键表格目视复核 + 外部资料交叉核证 2012 版对照）。实现人员**不得**凭厂商 SDK 头文件或旧版记忆自行扩大接口范围。

### 37.1.1 规范定位与总体形态

| 事实                                                                                     | 出处      |
| ---------------------------------------------------------------------------------------- | --------- |
| 服务端密码设备（密码机、密码卡、智能密码终端）向通用密码服务层提供基础密码服务的应用接口 | §6.1      |
| 采用 C 语言描述；参数长度单位均为字节                                                    | §6.1      |
| **符合本文件的密码设备应支持 ECC 算法（本文件中 ECC 特指 SM2 算法）、对称算法以及杂凑运算的函数接口** | §6.1 |
| 所有接口函数均应能被应用系统任意调用                                                     | §6.1      |
| 算法标识数值见 GM/T 0006；**分组密码算法的算法标识包含其工作模式**                        | §5.1      |
| 基本数据类型均为**高位字节在前（Big-Endian）**存储和交换                                  | §5.2      |
| 设备信息中 `StandardVersion` 本版本为 **2**                                              | §5.3 表2  |

### 37.1.2 生产类函数全集（6.2～6.7，共 61 个）

| 类别               | 数量 | 函数                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| ------------------ | ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 6.2 设备管理       | 8    | `SDF_OpenDevice`、`SDF_CloseDevice`、`SDF_OpenSession`、`SDF_CloseSession`、`SDF_GetDeviceInfo`、`SDF_GenerateRandom`、`SDF_GetPrivateKeyAccessRight`、`SDF_ReleasePrivateKeyAccessRight`                                                                                                                                                                                                                                                                                                                                                             |
| 6.3 密钥管理       | 16   | `SDF_ExportSignPublicKey_RSA`、`SDF_ExportEncPublicKey_RSA`、`SDF_GenerateKeyWithIPK_RSA`、`SDF_GenerateKeyWithEPK_RSA`、`SDF_ImportKeyWithISK_RSA`、`SDF_ExportSignPublicKey_ECC`、`SDF_ExportEncPublicKey_ECC`、`SDF_GenerateKeyWithIPK_ECC`、`SDF_GenerateKeyWithEPK_ECC`、`SDF_ImportKeyWithISK_ECC`、`SDF_GenerateAgreementDataWithECC`、`SDF_GenerateKeyWithECC`、`SDF_GenerateAgreementDataAndKeyWithECC`、`SDF_GenerateKeyWithKEK`、`SDF_ImportKeyWithKEK`、`SDF_DestroyKey`                                                                        |
| 6.4 非对称算法运算 | 7    | `SDF_ExternalPublicKeyOperation_RSA`、`SDF_InternalPublicKeyOperation_RSA`、`SDF_InternalPrivateKeyOperation_RSA`、`SDF_ExternalVerify_ECC`、`SDF_InternalSign_ECC`、`SDF_InternalVerify_ECC`、`SDF_ExternalEncrypt_ECC`                                                                                                                                                                                                                                                                                                                              |
| 6.5 对称算法运算   | 20   | `SDF_Encrypt`、`SDF_Decrypt`、`SDF_CalculateMAC`、`SDF_AuthEnc`、`SDF_AuthDec`、`SDF_EncryptInit/Update/Final`、`SDF_DecryptInit/Update/Final`、`SDF_CalculateMACInit/Update/Final`、`SDF_AuthEncInit/Update/Final`、`SDF_AuthDecInit/Update/Final`                                                                                                                                                                                                                                                                                                    |
| 6.6 杂凑运算       | 6    | `SDF_HMACInit`、`SDF_HMACUpdate`、`SDF_HMACFinal`、`SDF_HashInit`、`SDF_HashUpdate`、`SDF_HashFinal`                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 6.7 用户文件操作   | 4    | `SDF_CreateFile`、`SDF_ReadFile`、`SDF_WriteFile`、`SDF_DeleteFile`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |

附加部分：附录 B（规范性）SM9 算法 15 个函数；附录 C（规范性）VPN 设备 6 个函数。平台 V1.0 均不启用。

### 37.1.3 四条决定性事实（本设计的核心约束）

**事实一：标准不提供密钥对生成接口。**
表 11（密钥管理类函数）无 `SDF_GenerateKeyPair_RSA` / `SDF_GenerateKeyPair_ECC`；`SDF_GenerateKeyPair_*` 被移入 **6.8 验证调试类函数**。§5.4.1 明确："设备密钥只能在设备初始化时生成或安装，**用户密钥通过密码设备管理工具生成或安装**"。
**→ 平台在 HSM 模式下不能通过应用接口生成 SM2 密钥对，只能引用管理工具预置的密钥索引。**

**事实二：标准不提供明文对称密钥导入。**
2012 版的 `SDF_ImportKey` 已删除。会话密钥进入设备的唯一标准路径是带保护结构的导入：`SDF_ImportKeyWithKEK`（KEK 包裹）、`SDF_ImportKeyWithISK_ECC`（内部加密私钥解封）、`SDF_GenerateKeyWithIPK/EPK`（公钥封下来）。
**→ 平台"密钥不出安全边界"原则与标准完全一致，且必须以 KEK 包裹形态管理 Data Key。**

**事实三：标准不提供"内部私钥 ECC 数据解密"。**
表 12 非对称算法运算类函数仅 7 个（目视复核原文确认），6.4.1 概述自述其功能为"RSA公私钥运算、ECC签名验证**和加密**功能"——无解密。2012 版的 `SDF_ExternalDecrypt_ECC`（外部私钥解密）移入 6.8 调试类；`SDF_InternalDecrypt_ECC` 在 2012 版标准文本中即不存在（属厂商扩展）。
**→ 任意数据的 SM2 解密（需求 F-SM2-003）在纯标准接口下无对应函数**，唯一标准合规路径是数字信封语义（§37.6.3）。

**事实四：6.8 验证调试类函数禁止用于生产。**
6.8.1 原文："验证调试类函数**仅用于在调试、测试、检测场景下**对产品的算法和功能进行验证，**不用于实际密码服务**"。表 16 共 12 个函数：`SDF_GenerateKeyPair_RSA/ECC`、`SDF_ExternalPrivateKeyOperation_RSA`、`SDF_ExternalSign_ECC`、`SDF_ExternalDecrypt_ECC`、`SDF_ExternalSign_SM9`、`SDF_ExternalDecrypt_SM9`、`SDF_ExternalKeyEncrypt/Decrypt`、`SDF_ExternalKeyEncryptInit/DecryptInit`、`SDF_ExternalKeyHMACInit`（多数原型无会话句柄）。
**→ 平台任何生产代码路径禁止调用 6.8 全部函数；KAT 自检需另行设计（§37.6.5）。**

### 37.1.4 密钥载体与关键语义

| 事实                                                                                                                       | 出处     |
| -------------------------------------------------------------------------------------------------------------------------- | -------- |
| 密钥分三类载体：**密钥对索引**（非对称，索引 0=设备密钥，1 起为用户密钥，每索引含一个签名对 + 一个加密对）、**KEK 索引**（从 1 开始）、**会话密钥句柄**（接口函数生成或导入，用句柄检索） | §5.4     |
| 使用内部私钥前须 `SDF_GetPrivateKeyAccessRight`（索引、访问控制码，**控制码不少于 8 字节**），用毕 `SDF_ReleasePrivateKeyAccessRight` | §6.2.8/9 |
| `SDF_Encrypt` / `SDF_Decrypt` / `SDF_CalculateMAC` **不对数据做填充处理**；IV 长度与算法分组长度相同                        | §6.5.2/3/4 |
| `SDF_AuthEnc` / `SDF_AuthDec` 适用于 **CCM 与 GCM** 模式，输入输出按 GM/T 0006 算法标识与 GB/T 36624 确定；参数为 开始变量 S / AAD / 明文 / 密文 / 鉴别数据(Tag) | §6.5.5/6 |
| `SDF_HashInit(hSession, uiAlgID, pucPublicKey, pucID, uiIDLength)`：`uiIDLength` 非零且 `uiAlgID=SGD_SM3` 时执行 **SM2 预处理1（Z 值）**，此时 `pucPublicKey` 不能为空，过程符合 GB/T 35276 | §6.6.5   |
| `SDF_HMAC*` 三步式带密钥杂凑，过程符合 GB/T 15852.2                                                                        | §6.6.2/3/4 |
| `SDF_InternalSign_ECC` / `SDF_InternalVerify_ECC` / `SDF_ExternalVerify_ECC` 的输入为**待签数据的杂凑值**（SM2 时为签名预处理结果） | §6.4.5/6/7 |
| `SDF_InternalPublicKeyOperation_RSA` / `SDF_InternalPrivateKeyOperation_RSA` 的索引范围**仅限内部签名密钥对**              | §6.4.3/4 |
| `SDF_ImportKeyWithKEK` 的加密模式为 **ECB 模式**                                                                           | §6.3.16  |
| `SDF_DestroyKey` 销毁会话密钥并释放句柄资源，在对称算法运算后调用                                                          | §6.3.17  |
| 用户文件名最大 128 字节                                                                                                    | §6.7     |

### 37.1.5 数据结构要点

```c
/* 设备信息（§5.3）——平台能力声明的唯一事实来源 */
typedef struct DeviceInfo_st {
    CHAR  IssuerName[40];        /* 设备生产厂商名称        */
    CHAR  DeviceName[16];        /* 设备型号                */
    CHAR  DeviceSerial[16];      /* 设备编号                */
    ULONG DeviceVersion;         /* 设备内部软件版本号      */
    ULONG StandardVersion;       /* 接口规范版本号，本版本为 2 */
    ULONG AsymAlgAbility[2];     /* [0]=非对称算法标识按位或；[1]=支持的最大模长按位或 */
    ULONG SymAlgAbility;         /* 对称算法标识按位或      */
    ULONG HashAlgAbility;        /* 杂凑算法标识按位或      */
    ULONG BufferSize;            /* 支持的最大文件存储空间（字节） */
} DEVICEINFO;

/* ECC 密钥（§5.6）：ECCref_MAX_BITS=512，ECCref_MAX_LEN=64 */
typedef struct ECCrefPublicKey_st  { ULONG bits; BYTE x[64]; BYTE y[64]; } ECCrefPublicKey;
typedef struct ECCrefPrivateKey_st { ULONG bits; BYTE K[64]; }             ECCrefPrivateKey;

/* ECC 加密数据（§5.7）：结构序 = x||y||M(32)||L||C[L]，即 C1||C3||C2 */
typedef struct ECCCipher_st {
    BYTE  x[64];      /* 密文椭圆曲线点 x 分量（定长 64，大端，左侧补 0） */
    BYTE  y[64];      /* 密文椭圆曲线点 y 分量                            */
    BYTE  M[32];      /* 明文的杂凑值（SM3）                              */
    ULONG L;          /* 密文数据长度                                     */
    BYTE  C[];        /* 密文数据                                         */
} ECCCipher;

/* ECC 签名（§5.8）：定长 r||s，各 64 字节大端 */
typedef struct ECCSignature_st { BYTE r[64]; BYTE s[64]; } ECCSignature;

/* ECC 密钥对保护结构（§5.9）：密钥管理系统下发加密密钥对的保护结构 */
typedef struct EnvelopedECCKey_st {
    ULONG           Version;             /* 本版本为 1           */
    ULONG           ulSymmAlgID;         /* 必须为 ECB 模式      */
    ULONG           ulBits;              /* ECC 密钥对位长       */
    BYTE            cbEncryptedPrivKey[64]; /* 对称加密的私钥    */
    ECCrefPublicKey PubKey;
    ECCCipher       ECCCipherBlob;       /* 保护公钥加密的对称密钥 */
} EnvelopedECCKey;
```

**两个对平台编码契约有直接影响的结论**：

1. `ECCCipher` 的结构序就是 **C1C3C2**（x||y 即 C1，M 即 C3 杂凑，C 即 C2）。这与需求 V2.10 §3.1.1"SM2 默认密文编码 C1C3C2"一致，但 SDF 是**定宽大端**（x/y 各 64 字节左侧补零），而平台 REST API 的 C1C3C2 是**变宽**字节流。两者必须显式转换，禁止混用。
2. `ECCSignature` 是**定宽 RAW**（r、s 各 64 字节），需求默认签名编码为 **DER**。Provider 层必须实现 DER ↔ RAW 转换（现有 `SoftwareCryptoProvider` 已具备该能力，迁移时保留）。

### 37.1.6 错误码全集（附录 A，规范性）

`SDR_OK = 0x00000000`；`SDR_BASE = 0x01000000`。

| 值                | 宏                       | 说明               | 值                | 宏                       | 说明                 |
| ----------------- | ------------------------ | ------------------ | ----------------- | ------------------------ | -------------------- |
| SDR_BASE+0x01     | `SDR_UNKNOWERR`          | 未知错误           | SDR_BASE+0x11     | `SDR_FILESIZEERR`        | 文件长度超出限制     |
| SDR_BASE+0x02     | `SDR_NOTSUPPORT`         | 不支持的接口调用   | SDR_BASE+0x12     | `SDR_FILENOEXIST`        | 指定的文件不存在     |
| SDR_BASE+0x03     | `SDR_COMMFAIL`           | 与设备通信失败     | SDR_BASE+0x13     | `SDR_FILEOFSERR`         | 文件起始位置错误     |
| SDR_BASE+0x04     | `SDR_HARDFAIL`           | 运算模块无响应     | SDR_BASE+0x14     | `SDR_KEYTYPEERR`         | 密钥类型错误         |
| SDR_BASE+0x05     | `SDR_OPENDEVICE`         | 打开设备失败       | SDR_BASE+0x15     | `SDR_KEYERR`             | 密钥错误             |
| SDR_BASE+0x06     | `SDR_OPENSESSION`        | 创建会话失败       | SDR_BASE+0x16     | `SDR_ENCDATAERR`         | ECC加密数据错误      |
| SDR_BASE+0x07     | `SDR_PARDENY`            | 无私钥使用权限     | SDR_BASE+0x17     | `SDR_RANDERR`            | 随机数产生失败       |
| SDR_BASE+0x08     | `SDR_KEYNOTEXIST`        | 不存在的密钥调用   | SDR_BASE+0x18     | `SDR_PRKRERR`            | 私钥使用权限获取失败 |
| SDR_BASE+0x09     | `SDR_ALGNOTSUPPORT`      | 不支持的算法调用   | SDR_BASE+0x19     | `SDR_MACERR`             | MAC运算失败          |
| SDR_BASE+0x0A     | `SDR_ALGMODNOTSUPPORT`   | 不支持的算法模式调用 | SDR_BASE+0x1A   | `SDR_FILEEXISTS`         | 指定文件已存在       |
| SDR_BASE+0x0B     | `SDR_PKOPERR`            | 公钥运算失败       | SDR_BASE+0x1B     | `SDR_FILEWERR`           | 文件写入失败         |
| SDR_BASE+0x0C     | `SDR_SKOPERR`            | 私钥运算失败       | SDR_BASE+0x1C     | `SDR_NOBUFFER`           | 存储空间不足         |
| SDR_BASE+0x0D     | `SDR_SIGNERR`            | 签名运算失败       | SDR_BASE+0x1D     | `SDR_INARGERR`           | 输入参数错误         |
| SDR_BASE+0x0E     | `SDR_VERIFYERR`          | 验证签名失败       | SDR_BASE+0x1E     | `SDR_OUTARGERR`          | 输出参数错误         |
| SDR_BASE+0x0F     | `SDR_SYMOPERR`           | 对称算法运算失败   | SDR_BASE+0x1F     | `SDR_USERIDERR`          | 用户标识错误（2023 新增） |
| SDR_BASE+0x10     | `SDR_STEPERR`            | 多步运算步骤错误   | +0x20 ～ +0xFFFFFF | 保留                     |                      |

---

## 37.2 适配层总体结构

```text
CryptoPlatform.Crypto.Hsm / CryptoPlatform.Crypto.Software
├── SdfCryptoProvider            ← 唯一的 ICryptoProvider 实现（两模式共用，见第 36 章 §36.2）
├── SdfSessionPool               ← 会话池（借还、健康淘汰）
├── ISdfDevice                   ← 设备契约：开设备/开会话/设备信息
│    ├── SoftSdfDevice           ← 软件密码模块：BouncyCastle 实现 SDF 生产接口语义
│    └── NativeSdfDevice         ← HSM：P/Invoke 厂商 SDF 动态库
│         └── ISdfNativeLibrary  ← 厂商绑定边界（唯一的 P/Invoke 汇聚点）
└── Sdf/ （托管映射契约，位于 CryptoPlatform.Crypto.Abstractions）
     ├── ISdfSession              ← 61 个生产函数的托管映射
     ├── SdfDeviceInfo / SdfBlobs
     ├── SdfErrorCode / SdfAlgorithmId
     └── SdfKeyReference
```

**装配规则（部署模式互斥，V2.10 §5.6.3）**：

| 部署模式   | ISdfDevice 实现 | 准入前校验                                                     |
| ---------- | --------------- | -------------------------------------------------------------- |
| Software   | `SoftSdfDevice` | 主密码解封 Root Key → Wrap/Unwrap 测试 KEK → SelfTest（§3.11.3） |
| HSM        | `NativeSdfDevice` | `SDF_OpenDevice/OpenSession` → `SDF_GetDeviceInfo` 能力比对 → Key Identity/KCV 校验 → SelfTest |

---

## 37.3 ISdfDevice / ISdfSession 接口定义

### 37.3.1 设备契约

```csharp
namespace CryptoPlatform.Crypto.Abstractions.Sdf;

/// <summary>
/// GM/T 0018-2023 密码设备的托管映射（设备级）。
/// 对应 SDF_OpenDevice / SDF_CloseDevice / SDF_OpenSession / SDF_CloseSession / SDF_GetDeviceInfo。
/// 实现必须线程安全；会话对象非线程安全，由 SdfSessionPool 借还。
/// </summary>
public interface ISdfDevice : IAsyncDisposable
{
    string DeviceId { get; }
    SdfDeviceBinding Binding { get; }           // Soft / Native（含厂商、库路径、StandardVersion）

    /// <summary>SDF_OpenDevice。设备句柄由实现内部持有，不外泄。</summary>
    Task OpenAsync(CancellationToken ct);

    /// <summary>SDF_OpenSession → SDF_CloseSession（归还由池负责）。</summary>
    ISdfSession OpenSession();

    /// <summary>SDF_GetDeviceInfo。能力探测唯一入口。</summary>
    Task<SdfDeviceInfo> GetDeviceInfoAsync(CancellationToken ct);

    /// <summary>健康探测：OpenSession + GetDeviceInfo 往返计时。</summary>
    Task<SdfHealth> CheckHealthAsync(CancellationToken ct);
}
```

### 37.3.2 会话契约（生产函数全集映射）

```csharp
namespace CryptoPlatform.Crypto.Abstractions.Sdf;

/// <summary>
/// GM/T 0018-2023 会话级生产函数（6.2～6.7，61 个）的托管映射。
/// 所有方法在失败时抛出 SdfException（携带 SdfErrorCode 原始码）。
/// 字节序：实现负责与设备之间的大端转换，托管侧统一使用 byte[] 原始字节序。
/// </summary>
public interface ISdfSession : IDisposable
{
    // ── 6.2 设备管理 ──
    SdfDeviceInfo GetDeviceInfo();                                   // SDF_GetDeviceInfo
    byte[] GenerateRandom(int length);                               // SDF_GenerateRandom
    void GetPrivateKeyAccessRight(int keyIndex, string password);    // SDF_GetPrivateKeyAccessRight（≥8 字节）
    void ReleasePrivateKeyAccessRight(int keyIndex);                 // SDF_ReleasePrivateKeyAccessRight

    // ── 6.3 密钥管理（ECC = SM2）──
    SdfEccPublicKey ExportSignPublicKey_Ecc(int keyIndex);           // SDF_ExportSignPublicKey_ECC
    SdfEccPublicKey ExportEncPublicKey_Ecc(int keyIndex);            // SDF_ExportEncPublicKey_ECC
    SdfSessionKey GenerateKeyWithIpk_Ecc(int ipkIndex, int keyBits); // SDF_GenerateKeyWithIPK_ECC
    SdfSessionKey GenerateKeyWithEpk_Ecc(int keyBits, SdfEccPublicKey publicKey); // SDF_GenerateKeyWithEPK_ECC
    SdfSessionKey ImportKeyWithIsk_Ecc(int iskIndex, ReadOnlySpan<byte> wrappedKey); // SDF_ImportKeyWithISK_ECC
    SdfAgreementData GenerateAgreementDataWithEcc(int iskIndex, int keyBits, SdfEccPublicKey peerPub, string userId); // SDF_GenerateAgreementDataWithECC
    SdfSessionKey GenerateKeyWithEcc(...);                           // SDF_GenerateKeyWithECC
    SdfSessionKey GenerateAgreementDataAndKeyWithEcc(...);           // SDF_GenerateAgreementDataAndKeyWithECC
    SdfSessionKey GenerateKeyWithKek(int keyBits, SdfAlgorithmId algId, int kekIndex, out byte[] wrappedKey); // SDF_GenerateKeyWithKEK
    SdfSessionKey ImportKeyWithKek(SdfAlgorithmId algId, int kekIndex, ReadOnlySpan<byte> wrappedKey);        // SDF_ImportKeyWithKEK
    void DestroyKey(ISdfKeyHandle keyHandle);                        // SDF_DestroyKey
    // RSA 族 5 个（ExportSignPublicKey/ExportEncPublicKey/GenerateKeyWithIPK/GenerateKeyWithEPK/ImportKeyWithISK）
    // 平台 V1.0 不启用，实现可返回 SdfException(SdfErrorCode.SDR_NOTSUPPORT)。

    // ── 6.4 非对称算法运算 ──
    SdfEccSignature InternalSign_Ecc(int iskIndex, ReadOnlySpan<byte> hashedData);              // SDF_InternalSign_ECC
    bool InternalVerify_Ecc(int iskIndex, ReadOnlySpan<byte> hashedData, SdfEccSignature sig);  // SDF_InternalVerify_ECC
    bool ExternalVerify_Ecc(SdfAlgorithmId algId, SdfEccPublicKey publicKey, ReadOnlySpan<byte> hashedData, SdfEccSignature sig); // SDF_ExternalVerify_ECC
    SdfEccCipher ExternalEncrypt_Ecc(SdfAlgorithmId algId, SdfEccPublicKey publicKey, ReadOnlySpan<byte> data);   // SDF_ExternalEncrypt_ECC
    // RSA 运算 3 个：V1.0 不启用。

    // ── 6.5 对称算法运算 ──
    byte[] Encrypt(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> data);      // SDF_Encrypt（不填充）
    byte[] Decrypt(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> data);      // SDF_Decrypt（不填充）
    byte[] CalculateMac(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> data); // SDF_CalculateMAC（不填充）
    SdfAuthResult AuthEnc(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> startVar,
        ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext);                                                    // SDF_AuthEnc（CCM/GCM）
    byte[] AuthDec(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> startVar,
        ReadOnlySpan<byte> aad, ReadOnlySpan<byte> authTag, ReadOnlySpan<byte> ciphertext);                       // SDF_AuthDec
    ISdfMultiPacket EncryptInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv);                  // SDF_EncryptInit
    ISdfMultiPacket DecryptInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv);                  // SDF_DecryptInit
    ISdfMacMultiPacket CalculateMacInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> iv);          // SDF_CalculateMACInit
    ISdfAuthMultiPacket AuthEncInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> startVar,
        ReadOnlySpan<byte> aad, long? totalDataLength);                                                            // SDF_AuthEncInit
    ISdfAuthMultiPacket AuthDecInit(ISdfKeyHandle key, SdfAlgorithmId algId, ReadOnlySpan<byte> startVar,
        ReadOnlySpan<byte> aad, long? totalDataLength);                                                            // SDF_AuthDecInit

    // ── 6.6 杂凑运算 ──
    ISdfHash HashInit(SdfAlgorithmId algId, SdfEccPublicKey? signerPubKey, ReadOnlySpan<byte> userId);            // SDF_HashInit（userId 非空且 SGD_SM3 → SM2 预处理1）
    ISdfHash HmacInit(ISdfKeyHandle key, SdfAlgorithmId algId);                                                   // SDF_HMACInit（GB/T 15852.2）

    // ── 6.7 用户文件操作（V1.0 能力关闭）──
    void CreateFile(string name, int size);                          // SDF_CreateFile（名称 ≤128 字节）
    byte[] ReadFile(string name, int offset, ref int length);        // SDF_ReadFile
    void WriteFile(string name, int offset, ReadOnlySpan<byte> data);// SDF_WriteFile
    void DeleteFile(string name);                                    // SDF_DeleteFile
}
```

### 37.3.3 函数覆盖核对清单

实现完工时必须逐项核对：6.2（8/8）、6.3（ECC+KEK+DestroyKey 共 11/16 必须实现，RSA 5 个允许 NOTSUPPORT）、6.4（ECC 4/4 必须实现，RSA 3 个允许 NOTSUPPORT）、6.5（20/20）、6.6（6/6）、6.7（4/4，允许 NOTSUPPORT）。**6.8 的 12 个函数不得出现在本契约中**（见 §37.11）。

---

## 37.4 托管数据结构

```csharp
namespace CryptoPlatform.Crypto.Abstractions.Sdf;

/// <summary>算法标识透传值。数值权威来源为 GM/T 0006，由设备绑定层映射；本层不做数值定义。</summary>
public readonly record struct SdfAlgorithmId(uint Value)
{
    public override string ToString() => $"SGD(0x{Value:X8})";
}

public sealed record SdfDeviceInfo
{
    public string IssuerName { get; init; } = "";        // ≤40
    public string DeviceName { get; init; } = "";        // ≤16
    public string DeviceSerial { get; init; } = "";      // ≤16
    public uint DeviceVersion { get; init; }
    public uint StandardVersion { get; init; }           // 本标准应为 2
    public uint[] AsymAlgAbility { get; init; } = [0, 0];// [0]=算法按位或；[1]=最大模长按位或
    public uint SymAlgAbility { get; init; }
    public uint HashAlgAbility { get; init; }
    public uint BufferSize { get; init; }
}

/* ECC 结构：托管侧保存“变宽有效值”，编解码层负责 64 字节定宽大端左侧补零 */
public sealed record SdfEccPublicKey(int Bits, byte[] X, byte[] Y);      // 未压缩有效值 04||X||Y
public sealed record SdfEccPrivateKey(int Bits, byte[] K);
public sealed record SdfEccSignature(byte[] R, byte[] S);                // 有效长度 = (Bits+7)/8
public sealed record SdfEccCipher(byte[] X, byte[] Y, byte[] Hash, byte[] Cipher); // 结构序 C1||C3||C2
public sealed record SdfEnvelopedEccKey(uint Version, SdfAlgorithmId SymmAlgId, uint Bits,
    byte[] EncryptedPrivKey, SdfEccPublicKey PubKey, SdfEccCipher KeyCipher);

public sealed record SdfSessionKey(ISdfKeyHandle Handle, byte[]? WrappedKey, int KeyBits);
public sealed record SdfAuthResult(byte[] Ciphertext, byte[] AuthTag);
```

**编解码强制规则**：

1. `SdfEccPublicKey` ↔ `ECCrefPublicKey`：x/y 定宽 64 字节，**大端、左侧补零**，超出 64 字节即拒绝；
2. `SdfEccSignature` ↔ `ECCSignature`：r/s 定宽 64 字节大端；与平台 DER/RAW 变宽格式的转换属于 Provider 层职责；
3. `SdfEccCipher` ↔ `ECCCipher`：`L` 必须等于 `Cipher.Length`；与平台 REST 层 C1C3C2 变宽格式互转时按 `X[0..(Bits+7)/8]` 截取有效段；
4. 所有 `ULONG` 按 `sizeof(int)`（4 字节）读写，禁止按平台默认字节序。

---

## 37.5 平台密码运算 → SDF 函数映射矩阵

| 平台需求                       | 精确调用序列                                                                                         | 关键约定                                                         |
| ------------------------------ | ---------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| SM4 密钥生成（F-SM4-001）      | `SDF_GenerateKeyWithKEK(kekIndex=L2 KEK)`                                                            | 返回句柄 + KEK 密文；密文即平台 `WrappedMaterial`                |
| HMAC 密钥生成（F-HMAC-001）    | 同上（keyBits 按 HMAC 策略）                                                                          | 密钥类型经 `SDR_KEYTYPEERR` 兜底校验                             |
| SM4 加密 ECB/CBC/CTR（F-SM4-002） | `SDF_Encrypt`                                                                                      | **SDF 不填充**：PKCS#7 由 Provider 层做；CTR 需 GM/T 0006 对应标识（见 §37.13 待确认-2） |
| SM4 解密（F-SM4-003）          | `SDF_Decrypt`                                                                                        | 同上                                                             |
| SM4-GCM（V2.10 §3.1.3）        | `SDF_AuthEnc` / `SDF_AuthDec`                                                                        | 平台 12 字节 Nonce → `StartVar`；AAD → `pucAad`；Tag → `AuthData`；Tag 校验失败 → 平台 30002 |
| SM4 流式（F-SM4-004/005）      | `SDF_EncryptInit/Update/Final` / `SDF_DecryptInit/Update/Final`                                      | 多包句柄绑定会话，禁止跨会话使用；异常必须 Final 释放            |
| SM2 密钥对引用（F-SM2-001）    | `SDF_ExportSignPublicKey_ECC(idx)` / `SDF_ExportEncPublicKey_ECC(idx)`                               | 生成由设备管理工具完成（§37.1.3 事实一）；平台登记 `SdfKeyReference{KEYINDEX}` |
| SM2 加密（F-SM2-002）          | `SDF_ExportEncPublicKey_ECC` + `SDF_ExternalEncrypt_ECC`                                             | 输出 `SdfEccCipher`（C1C3C2 定宽）→ Provider 层转平台变宽格式     |
| SM2 解密（F-SM2-003）          | 数字信封：`SDF_ImportKeyWithISK_ECC` + `SDF_Decrypt`；纯数据解密走厂商扩展（能力 `SM2_DECRYPT_EXT`） | 见 §37.6.3                                                        |
| SM2 签名（F-SM2-004）          | `SDF_GetPrivateKeyAccessRight` → `SDF_HashInit(SGD_SM3, pubKey, ID)` + `Update` + `Final` → `SDF_InternalSign_ECC` → `SDF_ReleasePrivateKeyAccessRight` | ID 默认 `1234567812345678`，可配置；DER 转换在 Provider 层 |
| SM2 验签（F-SM2-005）          | `SDF_InternalVerify_ECC`（密钥在设备内）/ `SDF_ExternalVerify_ECC`（外部公钥）                        | 输入为预处理后的摘要值                                           |
| SM3（F-SM3-001/002）           | `SDF_HashInit(SGD_SM3, null, 空)` + `Update` + `Final`                                               | 天然流式；大数据禁止一次性装载                                   |
| HMAC-SM3（F-HMAC-001/002）     | `SDF_HMACInit` + `SDF_HMACUpdate` + `SDF_HMACFinal`                                                   | GB/T 15852.2；备选 `SDF_CalculateMAC`（分组 MAC，语义不同不得混用） |
| 随机数（F-RNG-001～003）       | `SDF_GenerateRandom`                                                                                  | 单次长度受平台配置上限约束（默认 64KB）                          |
| 会话密钥销毁                   | `SDF_DestroyKey`                                                                                      | 对称运算后必须调用；`DestroyKeyResult.OnlineMaterialDestroyed`   |
| 设备能力（F-DEV-002/004）      | `SDF_GetDeviceInfo`                                                                                   | 解析三能力字段 → `ProviderCapabilities`（§36.4.3）               |
| Root Key 轮换/重包裹（F-RK-004）| 无标准函数                                                                                            | 依赖设备管理工具/厂商扩展；缺失 → `NotSupported`                 |
| 密钥备份/恢复（F-RK-003）      | 无标准函数（Vendor Backup 属设备管理域）                                                              | 平台仅登记回执与元数据（V2.10 §3.11.5）                          |

---

## 37.6 关键设计决策

### 37.6.1 密钥对生成：管理工具预置 + 索引引用

**决策**：HSM 模式下，平台不实现"生成 SM2 密钥对"的设备操作；密钥对由密码设备管理工具预置到指定索引（每个索引一个签名对 + 一个加密对），平台在密钥开通流程中执行：

```text
管理端发起 SM2 密钥开通（指定设备与索引规划）
 ↓
设备管理工具预置（平台外，人工/厂商工具，产出索引号与访问控制码信封）
 ↓
平台登记：DeviceId + KeyIndex(SIGN/ENC) + 私钥访问控制码（经 L2 KEK 加密存储）
 ↓
SDF_ExportSignPublicKey_ECC / SDF_ExportEncPublicKey_ECC 导出公钥
 ↓
计算 SM3 指纹 → 与管理工具回执比对 → 生成 ProviderKeyIdentity（KCV）
 ↓
SDF_GetPrivateKeyAccessRight 验证访问控制码可用 → 释放
 ↓
KeyMaterial 落库（仅 ProviderReference / ProviderKeyIdentity，无明文材料）
```

**理由**：§37.1.3 事实一是标准强制，不是实现选择。任何"用 SDF_GenerateKeyPair_ECC 生成"的捷径都落入 6.8 调试类，违反规范且无法通过密评。需求 F-SM2-001"生成 SM2 公私钥对"在 HSM 模式下的语义随之调整为"开通并引用"，**该调整需回写需求 V2.10 §3.1.1（见 §37.13 待确认-1）**。

### 37.6.2 对称密钥：KEK 包裹为唯一进出形态

`SDF_GenerateKeyWithKEK` 的返回值（句柄 + KEK 加密密文）与平台 L2→L3 模型完全同构：句柄用于运算，密文用于持久化（`WrappedMaterial`）。因此：

1. 平台**不存在**"明文密钥落库"或"明文密钥导入设备"两条路径中的任何一条；
2. Data Key 恢复 = `SDF_ImportKeyWithKEK`（KEK 包裹态 → 句柄），与需求 §3.11.5 Software/HSM 恢复流程一致；
3. `CanWrapKey`/`CanUnwrapKey` 准入项可直接用 `SDF_GenerateKeyWithKEK` + `SDF_ImportKeyWithKEK` 往返校验（wrap 后 unwrap 比对 KCV）。

### 37.6.3 SM2 解密：数字信封优先，厂商扩展门控

**决策**：

1. **平台新业务 API 的 SM2 加密语义定义为数字信封**：`SDF_ExternalEncrypt_ECC` 产生的 `SdfEccCipher` 中的 `C` 段按"被保护的对称密钥/数据"解释。解密时若私钥在设备内，标准路径为 `SDF_ImportKeyWithISK_ECC`（解出被包封的会话密钥）+ `SDF_Decrypt`（解开数据）。
2. **纯"任意数据 SM2 密文解密"（非信封结构）**：标准无函数。若所选设备提供厂商扩展（如 2012 风格的 `SDF_InternalDecrypt_ECC`），必须：
   - 仅通过 `NativeSdfDevice` 的**显式扩展接口**暴露（不进入 `ISdfSession` 标准面）；
   - 能力声明增加 `SM2_DECRYPT_EXT`，经准入校验后启用；
   - 未声明该能力时，`/api/v1/crypto/sm2/decrypt` 返回明确 `NotSupported`（40007 段）并审计，**不得用外部私钥导出的方式实现**。
3. `SoftSdfDevice` 与 HSM 行为一致：同样只实现信封路径 + 可选扩展位。

**理由**：§37.1.3 事实三。把它做成本设计的一条显式边界，避免实现阶段被迫"抄近路"导出私钥。

### 37.6.4 GCM 与 Nonce 契约

`SDF_AuthEnc` 的 `StartVar` 即 GCM 的 IV。平台侧 V2.10 §3.1.3 的确定性 Nonce（KeyVersion||NodeId||MonotonicCounter，12 字节）由 Provider 层生成后传入 `StartVar`；设备不承担 Nonce 唯一性责任，Provider 必须保留 Nonce 使用记录与 2³¹/2³² 阈值逻辑。CTR 溢出与 GCM 调用总量告警同样在 Provider 层实现（设备无此语义）。

### 37.6.5 自检（F-KAT-001～004）与 6.8 函数禁用

标准生产接口没有"算法 KAT"函数，而自检又是需求硬约束。**决策**：

1. 上电/周期/按需自检的 KAT 由**平台侧测试向量库**执行，向量来自 GM/T 标准测试向量；
2. 向量执行的**运算通道**仍走生产接口（`SDF_Encrypt`/`SDF_Decrypt`/`SDF_Hash*`/`SDF_HMAC*`/`SDF_InternalSign_ECC`/`SDF_GenerateRandom`），即"用生产接口做已知答案测试"，不依赖 6.8 调试类；
3. SM2 KAT 需要外部私钥签名对照时，仅允许在**测试专用会话与测试专用密钥**上通过 6.8 函数执行，且该路径受配置开关 `SdfDebugFunctionsEnabled` 控制：
   - 默认 `false`，生产配置文件中禁止置 `true`（启动校验）；
   - 置 `true` 时仅 SoftSdfDevice 与测试环境装配，`NativeSdfDevice` 一律拒绝装配该路径；
   - 开关状态写入审计。
4. 自检失败 → Provider 状态置 `NOT_READY`，拒绝所有密码运算并产生安全事件（V2.10 §3.1.6）。

### 37.6.6 私钥访问控制码的生命周期

`SDF_GetPrivateKeyAccessRight` 是会话级授权：控制码不少于 8 字节、绑定会话、用毕释放。平台约定：

1. 访问控制码由设备管理工具生成，平台经 L2 KEK 加密存储（属 `KeyMaterial` 敏感字段，走完整性值保护）；
2. 会话池中的会话在借出时按需授权，归还时必须 `ReleasePrivateKeyAccessRight`；
3. 禁止在会话池的常驻会话上长期持有授权（最小权限）。

---

## 37.7 SDF 错误码映射

```csharp
public static class SdfErrorCodeMapper
{
    /// <summary>SDR_* → 平台 Provider 错误码。原始码保留在 SdfException 与审计明细。</summary>
    public static string Map(int sdfCode) => sdfCode switch
    {
        SdfErrorCode.SDR_OK                        => ProviderErrorCodes.SDR_OK,
        SdfErrorCode.SDR_COMMFAIL or SdfErrorCode.SDR_HARDFAIL
                                                   => ProviderErrorCodes.PROVIDER_DEVICE_OFFLINE,
        SdfErrorCode.SDR_OPENDEVICE or SdfErrorCode.SDR_OPENSESSION
                                                   => ProviderErrorCodes.PROVIDER_UNAVAILABLE,
        SdfErrorCode.SDR_PARDENY or SdfErrorCode.SDR_PRKRERR
                                                   => ProviderErrorCodes.PROVIDER_PRIVATE_KEY_ACCESS_DENIED,
        SdfErrorCode.SDR_KEYNOTEXIST               => ProviderErrorCodes.PROVIDER_KEY_NOT_FOUND,
        SdfErrorCode.SDR_KEYTYPEERR or SdfErrorCode.SDR_KEYERR
                                                   => ProviderErrorCodes.PROVIDER_KEY_INVALID,
        SdfErrorCode.SDR_NOTSUPPORT                => ProviderErrorCodes.PROVIDER_CAPABILITY_NOT_SUPPORTED,
        SdfErrorCode.SDR_ALGNOTSUPPORT or SdfErrorCode.SDR_ALGMODNOTSUPPORT
                                                   => ProviderErrorCodes.PROVIDER_CAPABILITY_NOT_SUPPORTED,
        SdfErrorCode.SDR_SIGNERR                   => ProviderErrorCodes.PROVIDER_OPERATION_FAILED,
        SdfErrorCode.SDR_VERIFYERR                 => ProviderErrorCodes.PROVIDER_VERIFY_FAILED,
        SdfErrorCode.SDR_MACERR                    => ProviderErrorCodes.PROVIDER_MAC_FAILED,
        SdfErrorCode.SDR_RANDERR                   => ProviderErrorCodes.PROVIDER_OPERATION_FAILED,
        SdfErrorCode.SDR_INARGERR or SdfErrorCode.SDR_OUTARGERR
                                                   => ProviderErrorCodes.PROVIDER_BAD_ARGUMENT,
        SdfErrorCode.SDR_STEPERR                   => ProviderErrorCodes.PROVIDER_STEP_ERROR,
        SdfErrorCode.SDR_NOBUFFER                  => ProviderErrorCodes.PROVIDER_NO_BUFFER,
        SdfErrorCode.SDR_PKOPERR or SdfErrorCode.SDR_SKOPERR or SdfErrorCode.SDR_SYMOPERR
            or SdfErrorCode.SDR_ENCDATAERR or SdfErrorCode.SDR_UNKNOWERR
                                                   => ProviderErrorCodes.PROVIDER_OPERATION_FAILED,
        _                                          => ProviderErrorCodes.PROVIDER_OPERATION_FAILED,
    };
}
```

补充规则：

1. `SDR_VERIFYERR` 同时覆盖 SM2 验签失败与 `SDF_AuthDec` 的 Tag 校验失败；后者在 Provider 层再细分为平台 30002（SM4_GCM_TAG_INVALID）；
2. `SdfException` 必须携带：原始 `int` 错误码、`SDF_*` 函数名、会话健康标记（决定会话是否回池）；
3. 厂商扩展错误码（超出附录 A 范围）统一映射为 `PROVIDER_OPERATION_FAILED`，原始值入日志。

---

## 37.8 会话池与设备生命周期

```csharp
public sealed class SdfSessionPool : IAsyncDisposable
{
    // V1.0 的 HsmConnectionPool 结构保留，语义修订如下：
    // 1. 池化对象 = ISdfSession（SDF_OpenSession 的句柄包装），不是“连接”；
    // 2. 借出 → 用毕归还；归还前执行 ReleasePrivateKeyAccessRight（若本会话做过私钥授权）；
    // 3. SdfException.Healthy == false 的会话（COMMFAIL/HARDFAIL）直接销毁并重建；
    // 4. MaxSessions 默认 = 设备规格上限（由 GetDeviceInfo/BufferSize 或厂商参数确定），禁止猜测值；
    // 5. 会话空闲超时回收；进程退出 DisposeAsync 时逐会话 SDF_CloseSession → SDF_CloseDevice；
    // 6. 主备设备：每台设备一个池，故障切换 = 切换活跃池（V2.10 F-DEV-005/006）。
}
```

**与 V1.0 的差异**：删除 `IHsmSdkAdapter.ConnectAsync/DisconnectAsync` 这类"连接"语义——SDF 的设备/会话就是连接语义本身，再抽象一层只会丢失 `SDR_OPENSESSION` 等错误码的精确性。

---

## 37.9 SoftSdfDevice（软件密码模块）

**定位**：以 BouncyCastle 实现 `ISdfDevice`/`ISdfSession` 的**全部生产接口语义**，是"软件密码模块模拟 HSM 行为"的落地物；同时它就是平台自测与密评演示的设备语义参考实现。

实现要点：

| SDF 语义               | 软件实现                                                                       |
| ---------------------- | ------------------------------------------------------------------------------ |
| 设备/会话              | 进程内对象；`OpenSession` 返回独立会话对象，保持错误码行为一致                  |
| 密钥存储区（索引型）   | 内存索引表 + 持久化（索引 → 加密材料），每个索引含签名对与加密对两个槽位        |
| KEK 索引               | 同上；L1/L2 分层与 §36.6.3 一致                                                |
| 会话密钥句柄           | 句柄 = 随机不透明 ID → 内存密钥槽；`DestroyKey` 后句柄失效（重复使用返回 `SDR_KEYNOTEXIST`） |
| GetPrivateKeyAccessRight | 校验 ≥8 字节控制码；错误计数与 `SDR_PARDENY` 行为对齐                         |
| `SDR_*` 错误码         | 全部按附录 A 语义抛出，禁止返回 `SDR_OK` 之外的自定义码                         |
| DEVICEINFO             | `StandardVersion=2`，能力字段按实际实现填充                                     |
| 填充                   | 与 HSM 一致：`Encrypt/Decrypt` 不填充，PKCS#7 在 Provider 层                    |

**禁用项**：`SoftSdfDevice` 不实现 6.8 调试类函数（除测试装配路径，§37.6.5）。

---

## 37.10 NativeSdfDevice 与厂商 SDK 隔离

```csharp
/// <summary>厂商 SDF 动态库绑定边界。这是全平台唯一允许出现 DllImport 的位置。</summary>
internal interface ISdfNativeLibrary
{
    int OpenDevice(out IntPtr hDevice);                                  // SDF_OpenDevice
    int CloseDevice(IntPtr hDevice);                                     // SDF_CloseDevice
    int OpenSession(IntPtr hDevice, out IntPtr hSession);                // SDF_OpenSession
    int CloseSession(IntPtr hSession);                                   // SDF_CloseSession
    int GetDeviceInfo(IntPtr hSession, out SdfDeviceInfoNative info);    // SDF_GetDeviceInfo
    /* ……其余 6.2～6.7 生产函数逐项声明，签名严格按 GM/T 0018-2023 原型…… */
}
```

强制规则：

1. **原生结构体 Marshal 集中**：`DEVICEINFO`、`ECCrefPublicKey`、`ECCCipher`、`ECCSignature`、`EnvelopedECCKey` 等结构体定义、大端编解码、定宽补零全部集中在绑定层；上层只见托管 record；
2. **厂商扩展隔离**：厂商私有函数（如内部 ECC 解密扩展）定义在 `IVendorSdfExtension`，仅 `NativeSdfDevice` 可引用，且必须映射为能力声明（§37.6.3）；
3. **函数缺失即失败**：绑定层按 StandardVersion=2 核对导出函数集；缺函数 → 设备准入失败，不得运行时降级；
4. 平台不依赖 Docker 与任何容器化方式加载厂商库；库路径、版本、商用密码产品认证证书编号按 F-DEV-008/009 登记。

---

## 37.11 验证调试类函数（6.8）禁用策略

| 规则     | 内容                                                                                             |
| -------- | ------------------------------------------------------------------------------------------------ |
| 禁用范围 | 表 16 全部 12 个函数，对 `ISdfDevice`/`ISdfSession` 契约不可见                                    |
| 唯一豁免 | 测试工程（`tests/*`）与自检 KAT 的测试专用路径（§37.6.5），受 `SdfDebugFunctionsEnabled` 控制     |
| 生产校验 | 启动时若检测到 `SdfDebugFunctionsEnabled=true` 且部署模式为生产 → 拒绝启动并记录安全事件         |
| 评审口径 | 密评材料中必须说明：平台不使用 6.8 函数提供密码服务，符合 GM/T 0018-2023 §6.8.1                  |

---

## 37.12 能力探测与准入校验流程

```text
设备注册（F-DEV-001）
 ↓
SDF_OpenDevice / SDF_OpenSession
 ↓
SDF_GetDeviceInfo → 解析 AsymAlgAbility / SymAlgAbility / HashAlgAbility
 ↓
映射为 ProviderCapabilities.Algorithms（§36.4.3）
 ↓
比对 sys_crypto_device.SupportedAlgorithms / ProviderCapabilities → 不一致即失败
 ↓
最小能力准入校验（CanWrapKey/CanUnwrapKey 实测：KEK 往返 wrap→unwrap→KCV 比对）
 ↓
自检（F-KAT）→ 通过置 READY
 ↓
准入结果与设备信息写入审计
```

健康检查（F-DEV-004）复用同一路径的轻量形态：`OpenSession → GetDeviceInfo → CloseSession` 往返计时；连续失败阈值沿用 Router 现有策略（3 次 DEGRADED / 5 次 OFFLINE）。

---

## 37.13 待确认事项清单

| # | 事项                                                                                             | 影响                                                             | 处理方式                                                     |
| - | ------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------- | ------------------------------------------------------------ |
| 1 | HSM 模式下"SM2 密钥对生成"语义调整为"管理工具预置 + 索引引用"（§37.6.1）                        | 需求 V2.10 §3.1.1 F-SM2-001、密钥开通流程、管理控制台交互         | 【待确认】需需求方与设备厂商确认后回写需求                   |
| 2 | **SM4-CTR 的 GM/T 0006 算法标识是否存在**：GM/T 0018-2023 只规定"算法标识见 GM/T 0006"，本项目未持有 GM/T 0006 文本。若 GM/T 0006 无 SM4-CTR 标识，则 HSM 场景 SM4-CTR 无法经标准 SDF 表达，需厂商扩展或 Provider 层以 ECB 计数器模式自行实现 | 需求 §3.1.3 SM4 模式支持、F-SM4-002/003 在 HSM 场景的落地        | 【待确认】获取 GM/T 0006 后冻结全部 `SdfAlgorithmId` 数值    |
| 3 | 所选 HSM 厂商是否提供"内部私钥 ECC 数据解密"扩展（§37.6.3）                                      | F-SM2-003 在 HSM 场景的可用性；REST `/crypto/sm2/decrypt` 语义    | 【待确认】选型时按 §37.6.3 门控验证                          |
| 4 | GM/T 0018-2023 删除 `SDF_ImportKey`/`SDF_GenerateKeyPair_*` 等函数未在前言"主要技术变化"中完整列明 | 与厂商对齐时可能出现"2012 兼容函数仍在 SDK 中"的情况             | 【待确认】准入核查时明确"仅按 2023 生产接口验收"             |
| 5 | RSAref_MAX_BITS 的数值（OCR 显示 2018，按上下文应为 2048）                                       | 仅影响 RSA 族（V1.0 不启用）                                     | 【待确认】启用 RSA 能力前核对原文                            |
| 6 | GM/T 0019-2023《通用密码服务接口规范》（docs 目录已存放扫描件，未纳入本轮分析）                   | 平台对外 REST API 的标准符合性论证，与 Crypto Provider 内部设计独立 | 【待确认】建议另行立项对齐                                   |
