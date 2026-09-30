# 36. ICryptoProvider 接口与 Provider 能力模型（基于 GM/T 0018-2023）

| 项目     | 内容                                                        |
| -------- | ----------------------------------------------------------- |
| 文档名称 | ICryptoProvider 接口与 Provider 能力模型 实现规范           |
| 文档版本 | V2.0                                                        |
| 编写日期 | 2026-09-30                                                  |
| 文档状态 | 已评审待定稿                                                |
| 上一版本 | V1.0（自定义高层抽象，未对齐密码设备接口标准）              |
| 文档用途 | 约束 `CryptoPlatform.Crypto.Abstractions` 与两处 Provider 实现的接口契约 |
| 合规基线 | GB/T 22239-2019（等保三级）、GB/T 39786-2021（密评三级）、信创 |
| 规范基线 | GM/T 0018-2023《密码设备应用接口规范》（本版核心依据）      |
| 需求基线 | 《国密加密服务系统软件需求规格说明书》V2.10 §2.6、§3.1、§3.11 |

> **本版修订说明（V2.0）**：V1.0 版的 `ICryptoProvider` 是一套自定义高层抽象（`ProviderType` / `GetCapabilities`），与任何密码设备接口标准均无对应关系，导致：① HSM 适配层无法按标准接口对接厂商 SDK；② 软件密码模块与 HSM 的行为一致性无法论证；③ 密钥对生成、密钥导入等关键能力的设备侧边界未定义。本版按 GM/T 0018-2023 重新设计，将设备接口语义下沉到独立的 SDF 适配层（第 37 章），Provider 层对齐需求 V2.10 §2.6。

---

## 36.1 设计基线与规范引用

### 36.1.1 规范引用

| 标准号         | 名称                       | 在本设计中的作用                                       |
| -------------- | -------------------------- | ------------------------------------------------------ |
| GM/T 0018-2023 | 密码设备应用接口规范       | 密码设备侧唯一接口契约；SDF 适配层的全部语义来源       |
| GM/T 0006      | 密码应用标识规范           | 算法标识（`uiAlgID`）数值的唯一权威来源                |
| GB/T 35276     | SM2密码算法使用规范        | SM2 签名预处理（Z 值）与密文/签名编码                  |
| GB/T 36624     | 可鉴别的加密机制           | SDF_AuthEnc / AuthDec（CCM、GCM）的输入输出定义        |
| GB/T 15852.2   | 消息鉴别码（专用杂凑函数） | SDF_HMACInit/Update/Final 的 HMAC 语义                 |
| GM/T 0003      | SM2椭圆曲线公钥密码算法    | SM2 算法行为                                           |
| GM/T 0004      | SM3密码杂凑算法            | SM3 算法行为                                           |
| GM/T 0002      | SM4分组密码算法            | SM4 算法行为                                           |

### 36.1.2 GM/T 0018-2023 的关键定位结论（设计前提）

以下结论均已对照规范原文核证（第 37 章给出逐条出处），是本设计的硬前提：

1. **GM/T 0018-2023 是设备层接口，不是业务层接口**。它采用 C 语言描述、句柄/会话模型、同步调用、`LONG` 错误码返回、`ULONG` 字节长度，且所有基本数据类型为**大端（Big-Endian）**。业务层不得直接依赖该形态。
2. **该标准明确"符合本文件的密码设备应支持 ECC（特指 SM2）算法、对称算法以及杂凑运算的函数接口"**（§6.1）。RSA 与 SM9 为附加能力，平台 V1.0 不启用。
3. **四个平台必需能力在生产接口中不存在**（详见第 37 章 §37.1.3）：密钥对生成、明文对称密钥导入、内部私钥 ECC 数据解密、外部私钥运算。这些能力在 2023 版中被删除或归入第 6.8 节"验证调试类函数"，而规范明确该类函数"仅用于调试、测试、检测场景，**不用于实际密码服务**"。

> **结论**：Provider 层不能把"设备能做什么"当作理所当然，必须以 GM/T 0018-2023 生产类函数全集（6.2～6.7，共 61 个函数）为能力上限，对超出部分显式声明能力并返回 `NotSupported`。这正是需求 V2.10 §1.7.13"能力声明与显式降级禁止"的设备侧落点。

### 36.1.3 需求侧硬约束（引自 V2.10，编码时不得违反）

| 约束                                                        | 来源        |
| ----------------------------------------------------------- | ----------- |
| 仅支持 Software / HSM 两种部署模式，互斥，不支持混合部署    | §5.6.3      |
| 软件密码模块模拟 HSM 行为，提供与 HSM 完全一致的接口        | §1.3.6、§5.6.2 |
| ProviderCapabilities 至少含 10 项能力，缺失能力返回 `NotSupported` 并审计 | §2.6        |
| CanWrapKey / CanUnwrapKey 为 Provider 准入强制项            | §2.6 最小能力准入集 |
| 平台逻辑密钥分层 L0~L3，物理实现由 Provider 解耦            | §3.11.1     |
| KeyMaterial 独立存储，HSM 场景仅保存 ProviderReference 等元数据，不保存明文材料 | §3.11.4     |
| Root Key 不直接参与业务 SM2/SM4 运算                        | §3.11.1     |
| 自检覆盖 SM2/SM3/SM4/HMAC-SM3/随机数，失败拒绝所有密码运算  | §3.1.6      |

---

## 36.2 分层架构

### 36.2.1 层次结构

```text
┌──────────────────────────────────────────────────────────┐
│  Application 层（CryptoService / KeyService / 设备管理）  │
└───────────────────────────┬──────────────────────────────┘
                            │ 仅依赖本层契约
┌───────────────────────────▼──────────────────────────────┐
│  CryptoPlatform.Crypto.Abstractions                      │
│  ├─ ICryptoProvider          平台统一 Provider 抽象       │
│  ├─ ProviderCapabilities     能力模型                     │
│  ├─ IKeyWrappingProvider 等  可选能力扩展接口             │
│  └─ Sdf/                     GM/T 0018-2023 托管映射契约  │
│     ├─ ISdfDevice / ISdfSession                          │
│     ├─ SdfDeviceInfo / SdfBlobs                          │
│     └─ SdfErrorCode / SdfAlgorithmId                     │
└──────────┬───────────────────────────────┬───────────────┘
           │                               │
┌──────────▼──────────────┐   ┌────────────▼───────────────┐
│ CryptoPlatform.Crypto.  │   │ CryptoPlatform.Crypto.Hsm  │
│ Software                │   │  NativeSdfDevice(P/Invoke) │
│  SoftSdfDevice          │   │  SdfSessionPool            │
│  (BouncyCastle 实现     │   │  厂商 SDK 仅出现在本工程    │
│   SDF 生产接口语义)     │   │                            │
└──────────┬──────────────┘   └────────────┬───────────────┘
           │                               │
           │            ┌──────────────────▼────────────────┐
           │            │ 厂商 SDF 动态库（.dll / .so）      │
           └───────────►│ 符合 GM/T 0018-2023，             │
                        │ StandardVersion = 2               │
                        └───────────────────────────────────┘
```

### 36.2.2 为什么 Provider 之下必须有一层 SDF 设备契约

1. **一份 Provider 实现，两种部署模式**。V1.0 设计要求 `SoftwareCryptoProvider` 与 `HsmCryptoProvider` 各自完整实现一遍 Provider（第 37 章 V1.0 亦如此），密码运算逻辑重复两份，且"软件模块模拟 HSM 行为"只能靠约定保证。改为：密码运算编排只写在 `SdfCryptoProviderBase` 一处，Software 模式注入 `SoftSdfDevice`，HSM 模式注入 `NativeSdfDevice`，行为一致性由"SDF 生产接口语义一致"在结构上保证。
2. **厂商替换收敛到绑定层**。不同 HSM 厂商 SDK 的差异（加载方式、句柄生命周期、扩展函数）全部收敛在 `NativeSdfDevice` 的 P/Invoke 绑定；上层契约不变。
3. **密钥载体语义有了标准出处**。GM/T 0018-2023 §5.4 定义了三类密钥载体：密钥对索引（非对称）、KEK 索引、会话密钥句柄。这三类恰好一一对应平台 L0~L3 分层模型（§36.6.3），平台无需再自造密钥引用格式。

---

## 36.3 ICryptoProvider 统一接口

### 36.3.1 目标契约（阶段二定稿形态）

```csharp
namespace CryptoPlatform.Crypto.Abstractions;

public interface ICryptoProvider
{
    // ── 身份与能力（需求 V2.10 §2.6）──
    string ProviderId { get; }                    // 与 sys_crypto_device.DeviceId 对应
    KeyStorageMode StorageMode { get; }           // Software / Hsm
    ProviderCapabilities Capabilities { get; }    // 事实来源：SDF_GetDeviceInfo（§36.4.3）

    // ── 密钥管理 ──
    Task<ProviderKeyResult> GenerateKeyAsync(KeySpec spec, CancellationToken ct);
    Task<ProviderKeyPairResult> GenerateKeyPairAsync(KeyPairSpec spec, CancellationToken ct);
    Task<ProviderKeyInfo> GetKeyInfoAsync(string providerKeyRef, CancellationToken ct);
    Task<DestroyKeyResult> DestroyKeyAsync(string providerKeyRef, DestroyKeyOptions? options, CancellationToken ct);

    // ── 密码运算 ──
    Task<CryptoResult> EncryptAsync(string providerKeyRef, ReadOnlyMemory<byte> plaintext,
        CryptoParameters parameters, CancellationToken ct);
    Task<CryptoResult> DecryptAsync(string providerKeyRef, ReadOnlyMemory<byte> ciphertext,
        CryptoParameters parameters, CancellationToken ct);
    Task<SignResult> SignAsync(string providerKeyRef, ReadOnlyMemory<byte> data,
        CryptoParameters parameters, CancellationToken ct);
    Task<bool> VerifyAsync(string providerKeyRef, ReadOnlyMemory<byte> data,
        ReadOnlyMemory<byte> signature, CryptoParameters parameters, CancellationToken ct);
    Task<byte[]> HashAsync(string algorithm, ReadOnlyMemory<byte> data, CancellationToken ct);
    Task<byte[]> MacAsync(string providerKeyRef, ReadOnlyMemory<byte> data, CancellationToken ct);

    // ── 随机数 / 设备 ──
    Task<RandomResult> GenerateRandomAsync(int length, CancellationToken ct);
    Task<ProviderHealth> CheckHealthAsync(CancellationToken ct);
    Task<SelfTestResult> SelfTestAsync(SelfTestRequest request, CancellationToken ct);
}
```

### 36.3.2 当前落地形态（阶段一，增量兼容）

为满足"不动现有业务实现"的约束，阶段一在现有 `ICryptoProvider` 上**只增不改**，通过 C# 默认接口成员（DIM）补充身份与能力契约；现有方法签名一律保留。差异如下：

| 目标契约成员                                  | 阶段一落地方式                                                     |
| --------------------------------------------- | ------------------------------------------------------------------ |
| `ProviderId`                                  | DIM：默认返回 `ProviderType`                                       |
| `StorageMode`                                 | DIM：由 `ProviderType` 推导（`"HSM"` → `Hsm`，其余 → `Software`）  |
| `Capabilities`                                | DIM：默认抛出 `NotImplementedException`（能力未声明即fail-fast，禁止静默空能力） |
| `SelfTestAsync(SelfTestRequest, ct)`          | DIM：默认抛出 `NotSupportedException`（对应错误码 30003 语义）     |
| `DestroyKeyAsync(ref, options, ct)` 返回结果  | 保留旧签名 `Task DestroyKeyAsync(ref, ct)`；结果语义由 `DestroyKeyResult` 在阶段二启用 |
| `GenerateKeyAsync(KeySpec, ct)` / `KeyPairSpec` | 阶段一保留 `KeyAlgorithm` / `KeyPairAlgorithm` 重载；`KeySpec` 在阶段二替换 |
| `MacAsync`                                    | 阶段一保留 `HmacAsync` 命名                                        |
| 流式 Hash / 流式对称加解密                    | 阶段一不下沉到 Provider 接口；由 `ISdfSession` 直接承载（F-SM3-002 / F-SM4-004/005 的大数据路径） |

**强制规则**：

1. 阶段一新增 Provider 实现必须覆写 `Capabilities` 与 `SelfTestAsync`，禁止依赖 DIM 默认值上线；
2. `Capabilities` 的 DIM 默认值**不允许**返回"全能力"或"空能力集合"之类的可被误用值；
3. 阶段二迁移时删除全部 DIM 兼容成员，`ProviderId` / `StorageMode` / `Capabilities` 变为必须实现。

---

## 36.4 ProviderCapabilities 能力模型

### 36.4.1 能力项定义（对齐需求 V2.10 §2.6）

```csharp
namespace CryptoPlatform.Crypto.Abstractions;

public sealed class ProviderCapabilities
{
    public bool CanWrapKey { get; init; }                        // 准入强制
    public bool CanUnwrapKey { get; init; }                      // 准入强制
    public bool CanBackupKey { get; init; }
    public bool CanRestoreKey { get; init; }
    public bool CanImportKey { get; init; }
    public bool CanExportKey { get; init; }                      // HSM 通常为 false
    public bool SupportsNonExportableKey { get; init; }
    public bool CanRotateRootKey { get; init; }
    public bool CanRewrapKey { get; init; }
    public bool SupportsKeyIdentityVerification { get; init; }   // KCV / Fingerprint / KeyIdentity

    // ── 算法能力（自 SDF_GetDeviceInfo 推导，见 36.4.3）──
    public IReadOnlySet<string> Algorithms { get; init; }        // 如 SM2 / SM4_ECB / SM4_GCM / SM3 / HMAC_SM3
}
```

### 36.4.2 能力项与 SDF 生产接口的对应关系

| 能力项                           | SDF 依据（GM/T 0018-2023）                                | 说明                                                         |
| -------------------------------- | --------------------------------------------------------- | ------------------------------------------------------------ |
| CanWrapKey / CanUnwrapKey        | `SDF_GenerateKeyWithKEK` / `SDF_ImportKeyWithKEK`         | 平台 L2→L3 保护的唯一标准路径                                |
| CanImportKey                     | `SDF_ImportKeyWithKEK` / `SDF_ImportKeyWithISK_ECC`       | **不存在明文导入**；导入必须带保护结构                       |
| CanExportKey                     | 仅会话密钥可经 KEK 加密导出；内部密钥索引不可导出         | HSM 默认 false                                               |
| SupportsNonExportableKey         | §5.4.1 内部密钥对（索引型）天然不出设备                   | HSM 为 true；SoftSdfDevice 同样为 true（模拟索引型）         |
| CanRotateRootKey / CanRewrapKey  | 无标准函数；依赖设备管理工具或厂商扩展                    | 缺失时按 NotSupported 处理                                   |
| SupportsKeyIdentityVerification  | `SDF_ExportSignPublicKey_ECC` / `SDF_ExportEncPublicKey_ECC` 派生指纹 | KCV 走对称运算自校验                                  |
| Algorithms                       | `SDF_GetDeviceInfo` → `AsymAlgAbility` / `SymAlgAbility` / `HashAlgAbility` | **能力声明的事实来源，禁止人工登记覆盖**      |

### 36.4.3 能力声明的唯一事实来源

**规则**：`ProviderCapabilities` 中算法类能力必须由 `SDF_GetDeviceInfo` 返回的 `DEVICEINFO` 按位或字段解析得到，并与 `sys_crypto_device.SupportedAlgorithms`、`ProviderCapabilities`（JSON 持久化，V2.10 §7.9）比对；两者不一致时设备注册/启用失败。

**理由**：人工登记的能力声明会随设备固件升级、模式不支持等原因失真；`DEVICEINFO` 是设备自己声明的、可随时探测的事实。这是需求 §1.7.13"能力声明与显式降级禁止"的机制化落点。

### 36.4.4 最小能力准入（强制）

```csharp
public static class ProviderCapabilitiesValidator
{
    /// <summary>Provider 注册与启用时强制调用；校验结果必须写入审计。</summary>
    public static AdmissionResult ValidateForAdmission(
        ProviderCapabilities capabilities, KeyStorageMode mode)
    {
        // 1. CanWrapKey && CanUnwrapKey 必须同时为 true（V2.10 §2.6 准入强制项）
        // 2. Algorithms 必须包含 SM2、SM3、SM4 家族、HMAC_SM3、RANDOM（§3.1）
        // 3. Hsm 模式下 CanExportKey 默认必须为 false，除非安全管理员显式审批
        // 4. 任一不满足 → 拒绝准入，不得降级使用
    }
}
```

---

## 36.5 可选能力扩展接口

对齐需求 V2.10 §2.6：非所有密码设备都必须实现以下能力，由 `ProviderCapabilities` 与可选接口共同决定。

```csharp
namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>密钥包裹/解包裹。准入强制能力（CanWrapKey/CanUnwrapKey）。</summary>
public interface IKeyWrappingProvider
{
    Task<WrappedKey> WrapKeyAsync(WrapKeyRequest request, CancellationToken ct);
    Task<UnwrapKeyResult> UnwrapKeyAsync(UnwrapKeyRequest request, CancellationToken ct);
}

/// <summary>Provider 级密钥备份（HSM 场景 = Vendor Backup / Security Domain Backup 的回执登记）。</summary>
public interface IKeyBackupProvider
{
    Task<BackupResult> BackupAsync(BackupRequest request, CancellationToken ct);
}

/// <summary>Provider 级密钥恢复。恢复后必须执行 KeyId → ProviderKeyIdentity → ProviderReference 身份校验。</summary>
public interface IKeyRestoreProvider
{
    Task<RestoreResult> RestoreAsync(RestoreRequest request, CancellationToken ct);
}

/// <summary>密钥导入。导入必须采用带保护结构的方式（见 36.9 映射矩阵第 4 条）。</summary>
public interface IKeyImportProvider
{
    Task<ProviderKeyResult> ImportKeyAsync(ImportKeyRequest request, CancellationToken ct);
}

/// <summary>密钥导出。HSM 通常不支持；支持时也仅允许 KEK 保护下的会话密钥导出。</summary>
public interface IKeyExportProvider
{
    Task<ExportedKey> ExportKeyAsync(ExportKeyRequest request, CancellationToken ct);
}

/// <summary>平台 Root Key / KEK 的轮换与重包裹契约（F-RK-004、F-KEK-003）。</summary>
public interface IRootKeyProvider
{
    Task<RotateRootKeyResult> RotateRootKeyAsync(RotateRootKeyRequest request, CancellationToken ct);
    Task<RewrapResult> RewrapAsync(RewrapRequest request, CancellationToken ct);
    Task<RootKeyStatus> GetRootKeyStatusAsync(CancellationToken ct);
}
```

**强制约束**（引自 V2.10 §2.6，编码时不得绕过）：

1. 平台不得因为某 Provider 不支持某项可选能力而绕过密码设备安全边界；
2. 不支持的能力必须返回明确的 `NotSupported` 结果并记录审计，不得静默失败；
3. 可选能力缺失时，管理控制台必须明示，对应管理端接口返回 `NotSupported`（错误码 40007 段）。

---

## 36.6 DTO 与密钥引用模型

### 36.6.1 密钥引用（ProviderKeyRef）

```csharp
/// <summary>
/// Provider 内部密钥引用。平台持久化的是“稳定标识”，运行期句柄不入库。
/// 格式：{ProviderId}:{Kind}:{Identity}
///   Kind=KEYINDEX  → Identity={index}:{role}       非对称密钥对索引（1..n，0 为设备密钥）
///   Kind=KEKINDEX  → Identity={index}              KEK 索引（1..n）
///   Kind=SESSION   → Identity={wrappedKeyId}       会话密钥（运行期经 Unwrap 获得句柄）
/// 示例：DEV-001:KEYINDEX:3:SIGN   DEV-001:KEKINDEX:1   DEV-001:SESSION:k-9f2c
/// </summary>
public sealed record SdfKeyReference(string ProviderId, SdfKeyKind Kind, string Identity)
{
    public override string ToString() => $"{ProviderId}:{Kind}:{Identity}";
}
```

### 36.6.2 密钥结果 DTO（阶段二形态）

```csharp
public sealed class ProviderKeyResult
{
    public string ProviderKeyRef { get; init; } = "";
    public string? PublicKeyMaterial { get; init; }      // 十六进制；SM2 时为 04||X||Y（65 字节）
    public string Fingerprint { get; init; } = "";        // SM3 指纹
    public string? DeviceId { get; init; }
    public string? EncryptedKeyMaterial { get; init; }    // Software 模式：KEK 包裹后的 WrappedMaterial
    public string? ProviderKeyIdentity { get; init; }     // HSM 模式：设备侧密钥身份（KCV/指纹）
}

public sealed class DestroyKeyResult
{
    public bool Success { get; init; }
    public bool OnlineMaterialDestroyed { get; init; }    // 设备内材料是否已销毁
    public bool ArchiveMaterialRetained { get; init; }    // 归档材料保留标记（V2.10 §3.2.23）
    public string? FailureReason { get; init; }           // 不支持/设备异常时的原因
}

public sealed class SelfTestResult
{
    public bool Passed { get; init; }
    public string OverallStatus { get; init; } = "UNKNOWN";   // PASSED / FAILED / NOT_SUPPORTED
    public IReadOnlyList<SelfTestItem> Items { get; init; } = [];  // SM2/SM3/SM4/HMAC_SM3/RNG 各项
    public DateTimeOffset ExecutedAt { get; init; }
}

public sealed record SelfTestItem(string Algorithm, bool Passed, string? Detail);
```

### 36.6.3 平台逻辑密钥分层 ↔ SDF 密钥载体映射

| 平台层级        | Software Provider 物理落地                            | HSM Provider 物理落地（SDF 载体）                       |
| --------------- | ----------------------------------------------------- | ------------------------------------------------------- |
| L0 KEK-Unlock   | 主密码 KDF 派生（PBKDF2-SM3）+ OS 安全存储            | 不实现（由 HSM Security Domain 访问控制替代）           |
| L1 Root Key     | Wrapped Root Key（KEK-Unlock 包裹，库内）             | 专用密钥对象：**密钥对索引或 KEK 索引**，不可导出        |
| L2 KEK-Runtime  | Wrapped KEK（Root Key 包裹），运行期解封              | **KEK 索引**（预置）或受 Root Key 保护的会话密钥         |
| L3 Data Key     | WrappedMaterial（KEK-Runtime 包裹）                   | **会话密钥句柄**：`SDF_GenerateKeyWithKEK` / `SDF_ImportKeyWithKEK` 获得 |
| SM2 业务密钥对  | 软件索引表（模拟密钥存储区）                          | **密钥对索引**（签名对 + 加密对，§5.4.1），私钥不出设备  |

**要点**：SDF 的"密钥对索引 / KEK 索引 / 会话密钥句柄"三类载体（GM/T 0018-2023 §5.4）与平台 L0~L3 分层是天然对应关系，`SdfKeyReference` 的三值枚举即来源于此；平台不需要自造第五种密钥载体。

---

## 36.7 错误码

### 36.7.1 Provider 统一错误码（保留 V1.0 集合，新增设备类）

```csharp
public static class ProviderErrorCodes
{
    public const string PROVIDER_UNAVAILABLE              = "PROVIDER_UNAVAILABLE";
    public const string PROVIDER_TIMEOUT                  = "PROVIDER_TIMEOUT";
    public const string PROVIDER_KEY_NOT_FOUND            = "PROVIDER_KEY_NOT_FOUND";
    public const string PROVIDER_KEY_INVALID              = "PROVIDER_KEY_INVALID";
    public const string PROVIDER_OPERATION_FAILED         = "PROVIDER_OPERATION_FAILED";
    public const string PROVIDER_DEVICE_OFFLINE           = "PROVIDER_DEVICE_OFFLINE";
    public const string PROVIDER_AUTH_FAILED              = "PROVIDER_AUTH_FAILED";
    public const string PROVIDER_CAPABILITY_NOT_SUPPORTED = "PROVIDER_CAPABILITY_NOT_SUPPORTED";

    // V2.0 新增
    public const string PROVIDER_NOT_SUPPORTED            = "PROVIDER_NOT_SUPPORTED";       // 显式 NotSupported（可选项缺失）
    public const string PROVIDER_PRIVATE_KEY_ACCESS_DENIED= "PROVIDER_PRIVATE_KEY_ACCESS_DENIED";
    public const string PROVIDER_SELF_TEST_FAILED         = "PROVIDER_SELF_TEST_FAILED";
    public const string PROVIDER_VERIFY_FAILED            = "PROVIDER_VERIFY_FAILED";
    public const string PROVIDER_MAC_FAILED               = "PROVIDER_MAC_FAILED";
    public const string PROVIDER_BAD_ARGUMENT             = "PROVIDER_BAD_ARGUMENT";
    public const string PROVIDER_STEP_ERROR               = "PROVIDER_STEP_ERROR";          // 多步运算步骤错误
    public const string PROVIDER_NO_BUFFER                = "PROVIDER_NO_BUFFER";
}
```

### 36.7.2 与平台错误码段的关系

Provider 错误码在 API 层映射到 V2.10 §13.3 的平台错误码段：

| Provider 错误码                          | 平台错误码 | 说明                     |
| ---------------------------------------- | ---------- | ------------------------ |
| PROVIDER_CAPABILITY_NOT_SUPPORTED / PROVIDER_NOT_SUPPORTED | 40007 段   | Provider NotSupported    |
| PROVIDER_KEY_NOT_FOUND                   | 40001      | KEY_NOT_FOUND            |
| PROVIDER_KEY_INVALID                     | 40006 段   | 密钥无效                 |
| PROVIDER_PRIVATE_KEY_ACCESS_DENIED       | 70005 段   | 私钥访问控制码错误       |
| PROVIDER_VERIFY_FAILED                   | 30005 段   | 验签失败                 |
| PROVIDER_OPERATION_FAILED / MAC_FAILED   | 30001 段   | CRYPTO_OPERATION_FAILED  |
| PROVIDER_SELF_TEST_FAILED                | 30003      | 密码模块自检失败         |
| PROVIDER_UNAVAILABLE / DEVICE_OFFLINE / AUTH_FAILED | 70000 段   | 密码设备不可用/认证失败  |
| PROVIDER_BAD_ARGUMENT                    | 20000 段   | 参数错误                 |

SDF 原始错误码（`SDR_*`）到 Provider 错误码的完整映射表见第 37 章 §37.7；**厂商原始错误码仅保留在日志与审计明细中，不得外泄到 API 响应**。

---

## 36.8 DI 注册与路由

```csharp
public static class CryptoProviderServiceCollectionExtensions
{
    public static IServiceCollection AddCryptoProviders(this IServiceCollection services)
    {
        services.AddSingleton<ICryptoProviderRouter, CryptoProviderRouter>();

        // 部署模式互斥（V2.10 §5.6.3）：启动时按配置二选一，装配错即拒绝启动
        // services.AddSingleton<ISdfDevice, SoftSdfDevice>();          // Software 模式
        // services.AddSingleton<ISdfDevice, NativeSdfDevice>();        // HSM 模式
        // services.AddSingleton<ICryptoProvider, SdfCryptoProvider>(); // 两种模式共用同一 Provider
        return services;
    }
}
```

路由语义与 V1.0 一致（按 `providerType`/设备解析 + 健康状态跟踪），但补充两条规则：

1. `ResolveProvider` 返回前必须校验 `ProviderCapabilities` 已通过准入（§36.4.4），未准入的 Provider 不得参与路由；
2. 部署模式下 `SdfCryptoProvider` 实例唯一；多设备（主备）通过 `ISdfDevice` 的多实例 + `ResolveByDevice` 表达，不得注册两个 `ICryptoProvider`。

---

## 36.9 Provider 操作 → 设备能力映射总表

详细论证与函数级映射见第 37 章 §37.5，此处给出 Provider 层视角的结论（含四条不可用结论的处置）：

| # | Provider 操作            | GM/T 0018-2023 生产接口支持 | 处置策略                                                     |
| - | ------------------------ | --------------------------- | ------------------------------------------------------------ |
| 1 | SM4/HMAC 密钥生成        | 支持                        | `SDF_GenerateKeyWithKEK`：生成即返回句柄 + KEK 密文，恰好等于平台"生成 Data Key 即得 WrappedMaterial" |
| 2 | **SM2 密钥对生成**       | **不支持**（6.8 调试类）    | 由密码设备管理工具预置到密钥索引；平台经 `SDF_ExportSignPublicKey_ECC` / `SDF_ExportEncPublicKey_ECC` 引用并登记 `SdfKeyReference` |
| 3 | SM4 加解密（ECB/CBC/CTR）| 支持                        | `SDF_Encrypt` / `SDF_Decrypt`；**SDF 不做填充**，PKCS#7 由 Provider 层负责 |
| 4 | SM4-GCM                  | 支持                        | `SDF_AuthEnc` / `SDF_AuthDec`（CCM/GCM）；平台 12 字节 Nonce 映射为 StartVar，AAD、Tag（AuthData）一一对应 |
| 5 | **明文对称密钥导入**     | **不支持**（已删除）        | 一律以 KEK 包裹形态导入（`SDF_ImportKeyWithKEK`），与平台 L2→L3 模型一致 |
| 6 | SM2 签名 / 验签          | 支持                        | `SDF_HashInit(pubkey, ID)` 完成预处理1（Z 值）→ `SDF_InternalSign_ECC` / `SDF_InternalVerify_ECC` / `SDF_ExternalVerify_ECC` |
| 7 | SM3 / HMAC-SM3           | 支持                        | `SDF_HashInit/Update/Final`、`SDF_HMACInit/Update/Final`（GB/T 15852.2） |
| 8 | 随机数                   | 支持                        | `SDF_GenerateRandom`                                         |
| 9 | **SM2 内部私钥数据解密** | **不存在标准生产函数**      | 标准合规路径为数字信封语义：`SDF_ImportKeyWithISK_ECC`（解出会话密钥）+ `SDF_Decrypt`；纯数据级 SM2 解密按厂商扩展能力门控（`Algorithms` 含 `SM2_DECRYPT_EXT` 才可用），否则返回 `NotSupported` |
| 10 | SM2 公钥加密（外部公钥） | 支持                        | `SDF_ExternalEncrypt_ECC`，输出 `ECCCipher`（结构序即 C1C3C2，见 §37.4） |
| 11 | 密钥销毁                 | 会话密钥支持                | `SDF_DestroyKey`；内部密钥索引的销毁属设备管理工具职责，Provider 返回 `DestroyKeyResult` 说明边界 |
| 12 | 能力探测 / 健康检查      | 支持                        | `SDF_GetDeviceInfo` / `SDF_OpenSession` 往返                 |
| 13 | 用户文件                 | 支持（V1.0 不启用）         | `SDF_CreateFile/ReadFile/WriteFile/DeleteFile`，能力关闭     |

---

## 36.10 破坏性变更与迁移策略

### 36.10.1 相对 V1.0 的变更清单

| 变更                                          | 类型       | 阶段 |
| --------------------------------------------- | ---------- | ---- |
| 新增 `Sdf/` 命名空间（ISdfDevice / ISdfSession / DTO / 错误码） | 新增       | 一（本版已落地） |
| 新增 `KeyStorageMode` / `ProviderCapabilities` / 6 个可选扩展接口 | 新增       | 一（本版已落地） |
| `ICryptoProvider` 增加 `ProviderId` / `StorageMode` / `Capabilities` / `SelfTestAsync`（DIM） | 新增（兼容） | 一（本版已落地） |
| `GetCapabilities()` 字符串集合 → `ProviderCapabilities` 强类型 | 破坏性     | 二   |
| `ProviderType`/`ProviderName` → `ProviderId`/`StorageMode`     | 破坏性     | 二   |
| `GenerateKeyAsync(KeyAlgorithm)` → `GenerateKeyAsync(KeySpec)` | 破坏性     | 二   |
| `DestroyKeyAsync` 返回 `DestroyKeyResult`                     | 破坏性     | 二   |
| `SoftwareCryptoProvider` 改为 `SdfCryptoProvider` + `SoftSdfDevice` 组合 | 破坏性     | 二   |
| 删除 `IHsmSdkAdapter` 自定义厂商防腐层（由 SDF 标准接口取代） | 破坏性     | 二   |

### 36.10.2 迁移原则

1. 阶段一（本版）只增不改，保证现有 `SoftwareCryptoProvider`、`CryptoProviderRouter` 及全部测试可编译、可运行；
2. 阶段二在 `SdfCryptoProvider` + `SoftSdfDevice` 通过与现实现等价的算法 KAT 之后切换，切换前两套实现并存；
3. V1.0 的 `IHsmSdkAdapter` 抽象**废弃**：厂商防腐职责由符合 GM/T 0018-2023 的 SDF 接口承担，"防腐"体现在厂商私有扩展只能通过 `ISdfDevice` 的显式扩展方法暴露并纳入能力门控，不得污染上层契约；
4. 所有阶段二破坏性变更落地时，同步更新需求追踪矩阵（V2.10 §13）。
