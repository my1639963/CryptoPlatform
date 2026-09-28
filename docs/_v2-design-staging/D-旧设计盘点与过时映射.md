# D. 旧设计盘点与过时映射

> **用途**：为架构师撰写新版详细设计文档的「设计变更说明」章节提供依据。
> **盘点对象**：`docs/` 下 12 份旧设计文档（只读，未修改）。
> **判定基线**：《国密加密服务系统软件需求规格说明书》V2.9（仓库内文件名 `需求说明文档.md`，5397 行）。
> **引用约定**：
> - 旧设计引用格式为 `文件名:行号`（行号取自本次盘点时的文件快照）。
> - 新要求在本文档中统一简称 **V2.9**，引用格式为 `V2.9 §小节 + (需求说明文档.md:行号)`。
> - 标记含义：【可继承】原文可基本沿用；【局部改】骨架可留、内容需改；【需重写】结论与 V2.9 冲突，须整体重做。
> - 所有结论均给出出处；无出处的判断不下结论。**未在旧设计中出现的机制一律标注"未实现"，不视为残留。**

---

## 第一部分　旧设计内容清单

### 1. `详细设计文档.md`（V1.1，3568 行，依据需求 V1.0）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 文档头 `:1-14` | 版本 V1.1；依据《国密加密服务平台软件需求说明书》**V1.0**；文档定位"直接进入详细设计" | 【需重写】依据文档与版本号须整体更换为 V2.9 基线 |
| 1.2 密钥对象模型 `:37-79` | Key / KeyVersion 两个对象模型；KeyId 生命周期不变；元数据与密钥材料分离 | 【可继承】模型分层正确，字段需按 V2.9 §7.2 补 |
| 1.3 密钥类型 `:81-92` | KeyType 表含 `SM2/SM4/HMAC/ROOT`，KeyUsage 含 SIGN/ENCRYPT/MAC/**WRAP**；APP_SECRET 独立为 ApplicationCredential | 【需重写】ROOT 不再是 KeyType、WRAP 不再是 KeyUsage（V2.9 §1.5、§3.2.1、§7.2） |
| 1.4 密钥用途约束 `:94-111` | 强制校验 `调用方→OwnerAppId→KeyType→KeyUsage→Operation`；KeyUsage 创建后不可改 | 【需重写】V2.9 明确"不校验 KeyUsage 业务语义"，改为 KeyType 兼容性校验（V2.9 §3.2.1、§3.1.7） |
| 1.5 密钥生成流程 `:113-142` | SM2/SM4/HMAC-SM3 三条生成流程；私钥不返回 | 【局部改】流程主干可用，需叠加 Provider 准入与 KeyMaterial 落库 |
| 1.6 密钥存储设计 `:144-240` | HSM 模式仅存 ProviderType/DeviceId/ProviderKeyRef/Fingerprint；软件模式 `DataKey→KEK→Root Key`；1.6.3 无 HSM 场景 Keystore 文件 + 主密码（PBKDF2-HMAC-SM3 ≥600,000 次）+ 部署安全等级表 | 【需重写】层级须改为 L0 Unlock KEK→L1 Root Key→L2 Runtime KEK→L3 Data Key；部署等级表（含开发测试明文、生产无 HSM 需评审）与 V2.9 冲突 |
| 1.7 密钥状态机 `:242-286` | Key 状态定义与转换表；"使用"不是状态；ROTATED/EXPIRED 默认仅历史解密验签；REVOKED"历史解密按策略" | 【局部改】Key 状态机可用；须补 KeyVersion 独立状态机（V2.9 §3.2.3 :901-934），并修正 REVOKED 历史解密语义（V2.9 §3.2.22 :1144） |
| 1.8 密钥轮换 `:288-315` | 逻辑 Key 不变、Version 递增；轮换步骤 8 步；触发条件含到期/次数/数据量/手工/安全事件 | 【可继承】与 V2.9 §3.2.10/§3.2.11 一致 |
| 1.9 密钥导入 `:317-338` | 区分公钥/私钥/对称密钥/HSM 迁移导入；明文不得经日志/URL；导入后进入 CREATED | 【局部改】须叠加 ProviderCapabilities 检查与 `NotSupported`（V2.9 §2.6 :612-618） |
| 1.10 备份与恢复 `:340-359` | HSM 依赖厂商备份；软件模式仅导出加密包装材料；恢复后 6 步验证 | 【局部改】须按 Provider 分别定义语义（V2.9 §3.11.5 :2092-2106） |
| 2.1-2.3 Provider 抽象与路由 `:365-431` | `ICryptoProvider` 统一抽象（Hsm/Software 两实现）；路由优先级 4 条；禁止未经策略自动转软件 | 【局部改】分层与"不自动降级"原则可继承；接口须拆为核心+可选扩展（V2.9 §2.6） |
| 2.4 HSM 设备抽象 `:433-463` | CryptoDevice 字段（DeviceId/Vendor/Model/SerialNumber/Endpoint/ProviderType/Status/Priority/IsPrimary/HealthCheckAt/ErrorCount）；设备状态四值 | 【局部改】字段须补 DeviceType/SupportedAlgorithms/ProviderCapabilities/CertNo/CertAuthority/CertExpireAt/MasterSlaveRelation/IntegrityValue（V2.9 §7.9） |
| 2.2 Provider 接口 `:377-413` | 同步版 ICryptoProvider 12 个方法；无 Capability 属性、无 SelfTest、无 Wrap/Unwrap/Backup | 【需重写】见第二部分 E 组 |
| 2.7 Provider 错误模型 `:492-507` | 8 个统一错误码（含 PROVIDER_CAPABILITY_NOT_SUPPORTED） | 【可继承】错误码枚举可用，需补 `NotSupported` 语义落点 |
| 3 数据库详细设计 `:511-706` | 设计原则（不存 HSM 私钥/对称密钥明文/AppSecret 明文/Access Token 明文）；sys_application、sys_application_secret、sys_key、sys_key_version、sys_crypto_device、sys_user/role/permission、sys_audit_log、sys_security_event | 【局部改】表骨架可继承；字段大面积缺失（见第二部分 C/D/J 组） |
| 3.4 审计表 `:653-685` | sys_audit_log 字段含 previous_hash/current_hash；禁存明文/私钥/AppSecret/Token | 【局部改】缺 ErrorCode 等字段（V2.9 §3.4.4 :1584-1587） |
| 4.1 认证分类与签名流程 `:712-821` | 管理 API=JWT Token；业务 API=appId+appSecret 签名；签名串=Method+Path+Timestamp+Nonce+BodyHash(Base64)；Timestamp ±5min；Nonce Redis 10min；失败 5 次/10min 锁定应用；旧 Secret 兼容 24 小时 | 【需重写】见第二部分 A/B/H 组 |
| 4.2-4.3 通用请求头与统一响应 `:823-863` | 两套请求头；响应 `{code,message,data,requestId,timestamp}` | 【局部改】响应体结构可继承；`code` 须改为数值分段（V2.9 §6.11 :2946-2961） |
| 4.4 Token 接口 `:865-898` | `POST /api/v1/admin/auth/token`；返回 accessToken/expiresIn 7200/role | 【需重写】路径与形态见 V2.9 §6.7 |
| 4.5-4.11 业务接口 `:900-1027` | 创建密钥、SM4 GCM/CBC、SM2 四操作、SM3、HMAC、随机数（POST） | 【局部改】路径与参数须对齐 V2.9 §6.5/§6.6；缺 algorithms/self-test |
| 4.12 幂等 `:1029-1045` | 幂等接口 5 项（rotate/revoke/destroy/token revoke/secret reset）；`Idempotency-Key` | 【局部改】清单须扩至 18 项（V2.9 §6.10.1 :2892-2914） |
| 5 认证与凭据 `:1049-1180` | 双认证模型总览表；AppSecret 生命周期与"不可逆哈希"存储；JWT 结构与校验 10 步；业务认证校验 11 步；Token 撤销 Redis jti；敏感操作二次确认 5 项 | 【需重写】见第二部分 B/F 组 |
| 6 审计与安全事件 `:1184-1287` | 审计对象 11 类；Operator 模型 4 类；RequestId/TraceId 透传；哈希链 `SM3(Log[n]+Hash[n-1])` + 检查点（"更高安全等级下"可签名）；审计不可篡改；安全事件类型与流程 | 【需重写】须改为 CanonicalSerialize + 分片聚合 + SM2 签名 + 外部锚点 + 集中外发（V2.9 §3.4.4） |
| 7 HA/灾备 `:1290-1371` | 应用层无状态；Redis 用途与 fail-close 原则；DB 主备；HSM 双设备；RTO≤30min / RPO≤5min | 【可继承】与 V2.9 §4.2/§4.3 无冲突 |
| 8 国产化部署 `:1374-1422` | 部署原则、适配矩阵、NTP 时间同步、数据库最小权限 4 账号 | 【局部改】须补国密 TLS 落地策略（V2.9 §8.2 :3716-3732） |
| 9 测试与验收矩阵 `:1426-1556` | 密钥管理/Provider/安全/密码算法/性能验收项；需求追踪矩阵示例 | 【局部改】骨架可继承，须补 V2.9 第 10 章新增用例（完整性、签名专项、Root Key、GCM Counter、审计链） |
| 10 设计约束与待确认项 `:1558-1623` | HSM/国产库/算法格式待确认清单；ProviderCapability 表（9 项能力） | 【可继承】待确认清单可直接作为新设计输入 |
| 11 关键安全边界 `:1627-1666` | 安全边界图；"业务系统持有应用身份凭据和 KeyId" | 【局部改】图中"appId+签名"需改为"请求直接签名"表述 |
| 13 密钥管理可编码级 `:1693-1924` | Key/KeyVersion 关系约束 8 条；创建事务 Saga；激活行锁；轮换并发控制；两阶段销毁；轮换后历史数据版本携带 | 【可继承】事务/并发/两阶段销毁骨架质量高，可直接沿用 |
| 14 Provider 可编码级 `:1926-2078` | 分层结构；推荐异步接口；ProviderKeyRef 规则 4 条禁止；Capability 清单 20 项；超时层级与重试策略 | 【局部改】Capability 需升级为 ProviderCapabilities + 最小准入集；接口需拆可选扩展 |
| 15 数据库完整字段 `:2080-2170` | sys_key / sys_key_version / sys_key_authorization / Application 与 Secret 字段定义 | 【局部改】字段须补 ProviderId/StorageType/ProviderKeyIdentity/MaterialVersion/IntegrityValue；删 key_usage |
| 16 核心 DDL 基线 `:2172-2240` | sys_key / sys_key_version / sys_key_authorization 建表语句；不建外键 | 【可继承】DDL 骨架可作新 DDL 起点 |
| 17 EF Core 基线 `:2244-2314` | SysKey / SysKeyVersion 实体 + Fluent API；row_version 乐观并发 | 【可继承】配置风格可沿用 |
| 18 API 事务与幂等 `:2318-2388` | 幂等键作用域 `AppId+Method+Path+Key`；sys_idempotency_record 字段；错误分类 9 类 | 【局部改】错误分类须映射到 V2.9 数值分段 |
| 19 SM4-GCM 约束 `:2390-2436` | 请求/响应 JSON；Nonce 允许调用方提供；AAD；Tag 失败四步处置 | 【需重写】Nonce 须平台强制生成并采用确定性唯一性（V2.9 §3.1.3 :719、:732-749） |
| 20 SM2 约束 `:2440-2472` | 曲线/签名编码/密文编码三概念分离；DER 或 R\|\|S；C1C3C2/C1C2C3；未实测不得承诺 | 【局部改】须补长度限制 512 字节默认值与 20001（V2.9 §3.1.1 :663-669） |
| 21 计量与轮换策略 `:2476-2504` | operation_count / processed_bytes / last_used_at；阈值触发异步轮换 | 【局部改】须补平台级全节点 2³² 总量约束（V2.9 §3.1.3 :748） |
| 22 安全事件模型 `:2508-2562` | 事件分类 14 类；严重等级 5 级；处置状态机 | 【局部改】分类须补 NodeId 冲突/外部锚点失败/数据完整性等（V2.9 §3.5.3 :1691-1695） |
| 23 第一阶段编码顺序 `:2566-2631` | 12 步编码顺序；8 条禁止事项 | 【可继承】编码顺序可作为新版实施顺序底稿 |
| 25 开发级数据模型 `:2674-2798` | 实体关系图；sys_key / sys_key_version / sys_key_authorization 最终字段 | 【局部改】同上 15 章 |
| 26 KeyService 接口 `:2800-2957` | IKeyService 8 方法（无 Backup/Restore/VerifyRestore/Expire）；Create/Rotate/Destroy 事务边界 | 【局部改】须补备份/恢复/恢复验证/到期处理（V2.9 §6.6 :2817-2819） |
| 27 EF Core 10 配置 `:2959-3044` | SysKey 实体与 Fluent API 完整配置 | 【可继承】 |
| 28 CryptoProvider 开发级 `:3046-3129` | CryptoOperationContext；接口 12 方法；Provider 禁止事项 7 条 | 【局部改】禁止事项可继承；接口须重写 |
| 29 密钥访问授权算法 `:3131-3187` | 管理 API 13 步 / 业务 API 15 步校验链；状态-操作矩阵 | 【需重写】须按 V2.9 §8.4 检查顺序（签名→防重放→资源归属→KeyType 兼容→状态→完整性值） |
| 30 密钥包装体系 `:3189-3247` | 软件模式 `Root Key→KEK→Encrypted Key Material`；HSM 模式 ProviderKeyRef | 【需重写】见 D 组 |
| 31-33 API 请求对象/并发补偿/异常模型 `:3249-3418` | 创建密钥、SM4-GCM、大数据会话接口；幂等与 UNKNOWN；统一错误结构 | 【局部改】骨架可用，字段与错误码须对齐 V2.9 |
| 34 代码分层 `:3420-3476` | 11 个项目分层与依赖方向；禁止 Api→HSM SDK / Api→DbContext | 【可继承】可直接沿用 |
| 35 任务分解 `:3478-3498` | 16 阶段任务表 | 【局部改】须插入完整性/Provider 能力/AdminSession/Short Cache 等任务 |
| 36 冻结项与待确认项 `:3500-3534` | 10 条可冻结（含"双认证模型"、"无 HSM 主密码方案"）；14 条联调前确认 | 【需重写】冻结项中双认证模型表述、Root Key 方案均须重写 |
| 37 结论 `:3536-3568` | 设计下沉路径总结 | 【可继承】 |

### 2. `核心模块详细设计与实现规范.md`（V1.0，923 行）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 文档头 `:1-10` | 版本 V1.0，依据《详细设计文档》V1.1 | 【需重写】基线版本须更新 |
| 1.1 枚举定义 `:26-64` | 8 组枚举（application status、secret status、key status、provider_type、authorization status、permissions、device status、operator_type、severity、event status） | 【局部改】须删 ROOT/KeyUsage；补 MFA/StorageType/KeyType 兼容错误等 |
| 1.2 敏感字段标识 `:66-77` | 6 个敏感字段及安全要求；全局禁止存储项 6 条 | 【局部改】secret_hash 行须改为 SecretCiphertext+SecretHash；须补 IntegrityKey |
| 1.3 表结构总览 `:79-109` | **14 张表**清单与核心字段；主键雪花 ID、无外键、owner_app_id 存 app_id、多对多角色、updated_at 拦截器、索引长度提示 | 【局部改】表数须扩至 22+；骨架约束（雪花 ID/无外键/关联方式）可继承 |
| 1.4 国产数据库适配 `:111-124` | MySQL/达梦/金仓三列对比（主键、datetime 精度、大字段、JSON、索引长度、布尔、ON UPDATE、字符集） | 【可继承】适配矩阵质量高，可直接复用 |
| 2.1 IKeyService `:130-157` | 10 方法（含 Import/GetVersions，无 Backup/Restore）；4 条与旧版差异说明 | 【局部改】须补备份/恢复/恢复验证 |
| 2.2.1 Create `:161-201` | Saga 补偿：DB 提交 Key → Provider 生成 → DB 写 Version；补偿任务表 + 3 次指数退避 + 孤儿密钥销毁 + CRITICAL 事件 | 【可继承】补偿设计完整，可直接沿用 |
| 2.2.2-2.2.6 `:203-284` | Activate（行锁+健康检查）、Disable、Rotate（分布式锁+FOR UPDATE+重新读取 current_version）、Revoke、Expire（每分钟扫描） | 【可继承】 |
| 2.2.7 Destroy `:286-311` | 两阶段销毁（申请 15 分钟有效 + 确认）；destroy_result 四值；清除 encrypted_key_material | 【局部改】须叠加高风险二次确认与 Provider 能力检查 |
| 2.2.8 Import `:313-323` | 格式/算法校验、CREATED 落库、KEK 加密或 HSM 安全导入、SM3 指纹 | 【局部改】须叠加 ProviderCapabilities/NotSupported |
| 2.2.9-2.2.11 `:325-353` | Backup/加密备份、Restore/完整性校验、VerifyRestore/可用性验证 | 【局部改】须按 Provider 分语义 |
| 3.1 ICryptoService `:359-397` | 11 方法；4 条差异说明（返回值结构化、随机数、流式 SM3、record DTO） | 【可继承】 |
| 3.2 通用授权校验 `:399-427` | 管理 API 路径 3 步 + 业务 API 路径 3 步 + 通用密钥授权 10 步；"安全策略允许（配额、限流）" | 【需重写】顺序与内容须按 V2.9 §8.4；配额/限流须删除（F-AUTH-022 已废弃） |
| 3.3 SM2 `:429-461` | 加解密（明文 ≤4096 bytes 或按能力）；签名格式 DER/Raw；ROTATED/EXPIRED 允许解密验签 | 【局部改】4096 须改为 512 默认值 |
| 3.4 SM3/HMAC `:463-488` | SM3 HEX 默认、空数据支持、流式；HMAC 常量时间比较 | 【可继承】 |
| 3.5.1-3.5.2 SM4 ECB/CBC `:492-508` | ECB 不推荐；CBC IV 校验与 PKCS#7 | 【可继承】 |
| 3.5.3 SM4 CTR `:510-517` | 仅要求"Nonce 不得重复"、"加解密使用相同 Nonce" | 【需重写】须补 16 字节计数器、大端 +1、禁止回绕、短窗口重复检测（V2.9 §3.1.3 :721-730） |
| 3.5.4 SM4 GCM `:519-539` | Nonce 推荐 12 字节、调用方可传入；AAD；Tag 失败四步 | 【需重写】Nonce 须平台强制生成 + 12 字节拼接结构 |
| 4.1 分层与物理隔离 `:543-579` | Abstractions / Software / Hsm.VendorX 三项目隔离；业务层只引用 Abstractions | 【可继承】防腐层设计优秀 |
| 4.2 ICryptoProvider `:581-620` | 12 方法 + `IReadOnlySet<string> GetCapabilities()` | 【需重写】见 E 组 |
| 4.3 Router `:622-645` | RouteForExistingKeyAsync / RouteForNewKeyAsync / GetAllProviders；路由优先级 4 条 | 【局部改】须按 ProviderCapabilities 与 StorageMode 解析 |
| 4.4 Provider 边界 `:647-655` | HSM/软件在密钥存储、使用场景、降级、销毁、备份的差异 | 【局部改】须按部署模式互斥（V2.9 §5.6.3）重述 |
| 4.5 厂商 SDK 隔离 `:657-679` | 新增厂商 8 步，不修改业务层 | 【可继承】 |
| 4.6 错误码映射 `:681-696` | 8 个 PROVIDER_* 错误码 + 厂商码映射内部保留 | 【可继承】 |
| 4.7 超时与重试 `:698-714` | API 30s > Service 15s > Provider 5-10s；Hash/Verify 可重试 2 次；非幂等禁止重试 | 【可继承】 |
| 4.8 健康检查 `:716-734` | 检查频率（路由前/30s 定时/失败触发）；3 次失败 DEGRADED、5 次 OFFLINE | 【可继承】 |
| 5.1-5.5 技术栈 `:738-791` | .NET 10 / C# 14 / EF Core 10 / MySQL（Pomelo 不支持 EF Core 10）/ 国产中间件替代说明 / BouncyCastle / JWT / Serilog / Scalar | 【局部改】JWT 行须改为 SM2 签名 Token；须补安全缓冲区、SM3 哈希链依赖 |
| 5.6 项目结构 `:793-810` | 13 个项目 | 【可继承】 |
| 5.7 依赖方向 `:812-834` | 允许/禁止依赖清单 | 【可继承】 |
| 6.1 双认证模型 `:842-850` | 路径前缀、认证方式、凭据存储、请求头对照表 | 【需重写】见 A/F 组 |
| 6.2 管理 API 认证流程 `:852-867` | 6 步登录流程；JWT claims 设计 | 【需重写】缺验证码/ MFA/AdminSession/RefreshToken |
| 6.3 业务 API 签名流程 `:869-883` | 旧签名串公式；`用 secret_hash 校验签名（PBKDF2 还原 → HMAC-SM3 → 常量时间比较）` | 【需重写】公式与存储模型双错 |
| 6.4 密钥授权校验器 `:885-901` | IKeyAuthorizationChecker + 4 步校验（Owner 或显式授权 + permissions 含 operation） | 【可继承】接口可留，须叠加资源归属与 KeyType 校验 |
| 附录 A 文档索引 `:905-923` | 9 份实现规范（第 32-40 章）索引 | 【局部改】须重排章节号 |

### 3. `实现规范-CryptoProvider接口.md`（第 36 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 36.1 ICryptoProvider `:5-43` | 13 个成员（含 `GetCapabilities()`）；无 ProviderId/StorageMode/Capabilities 属性；无 SelfTest/Wrap/Unwrap/Backup/Restore/Import/Export/RootKey | 【需重写】见 E 组 |
| 36.2 ICryptoProviderRouter `:45-61` | GetDefaultProvider / ResolveProvider(SysKeyVersion) / ResolveByDevice | 【局部改】须支持能力与 StorageMode 解析 |
| 36.3 DTO `:63-129` | CryptoParameters（Mode/Nonce/Aad/Tag/Padding/SignatureFormat/CipherFormat）、CryptoResult、SignResult、RandomResult、ProviderKeyResult（含 EncryptedKeyMaterial）、ProviderKeyPairResult、ProviderKeyInfo、ProviderHealth | 【局部改】须补 ProviderKeyIdentity/KCV/MaterialVersion 字段；须补 SelfTestResult/DestroyKeyResult/WrapResult |
| 36.4 枚举 `:131-142` | KeyAlgorithm = SM2 / SM4_128 / HMAC_SM3（**无 ROOT**，与 33 章 KeyType 不一致） | 【局部改】须统一 KeyType 枚举 |
| 36.5 错误码 `:144-169` | 8 个常量 + CryptoProviderException | 【可继承】 |
| 36.6 Router 实现 `:171-217` | GetDefaultProvider 返回 `_providers.First()`；ResolveProvider 按 ProviderType 匹配；ResolveByDevice 查表 | 【局部改】默认 Provider 取 First() 属实现缺陷，须按配置与能力解析 |
| 36.7 DI `:219-233` | AddCryptoProviders 注册 Router + 注释掉具体 Provider | 【可继承】 |

### 4. `实现规范-CryptoService.md`（第 35 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 35.1 请求/响应 DTO `:5-50` | SM4（含 Nonce 入参）、SM2、SM3（含流式）、HMAC、Random 全部 DTO | 【局部改】SM4 Nonce 须改为平台生成；须补 appId、长度限制字段 |
| 35.2 ICryptoService `:52-80` | 11 方法 | 【可继承】 |
| 35.3 实现 `:84-291` | SM4/SM2/SM3/HMAC/Random 实现骨架；`ResolveKeyAsync` 授权+状态校验；Decode/Encode 编解码 | 【局部改】须插入完整性值验证、KeyType 兼容校验、Nonce/Counter 生成、随机数长度上限 |
| `:248-275` ResolveKeyAsync | 状态判定：ACTIVE 全允许；ROTATED 仅 DECRYPT/VERIFY；其余拒绝 | 【局部改】须按 V2.9 §3.2.22 覆盖 EXPIRED/REVOKED 历史解密验签、HMAC Generate 归入"签名类" |
| `:239-244` Random | 无长度上限校验 | 【局部改】须补 64KB 默认 / 1MB 上限 + 20001 |
| `:265-272` 状态-操作 | ROTATED 允许 DECRYPT/VERIFY | 【局部改】须扩展至 EXPIRED/REVOKED |

### 5. `实现规范-EFCore实体与配置.md`（第 33 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 33.1 枚举 `:7-83` | 10 个枚举；`KeyType { SM2, SM4, HMAC, ROOT }`、`KeyUsage { SIGN, ENCRYPT, MAC, WRAP }` | 【需重写】须删 ROOT/WRAP，补 StorageType/MfaType/KeyMaterialStatus 等 |
| 33.2 实体 `:87-304` | 14 个实体类（SysApplication、SysApplicationSecret、SysKey、SysKeyVersion、SysKeyAuthorization、SysCryptoDevice、SysUser、SysRole、SysPermission、SysUserRole、SysAuditLog、SysSecurityEvent、SysConfig、SysIdempotencyRecord） | 【局部改】缺 11 个实体（KeyMaterial、SignatureNonce、UserMfaBinding、AdminSession、RefreshToken、UserDataDEK、RootKeyMetadata、KekMetadata、BackupRecord、AlertRule、IntegrityScanRecord） |
| `:104-115` SysApplicationSecret | 仅 SecretHash + SecretVersion | 【需重写】须为 SecretCiphertext + SecretHash |
| `:138-160` SysKeyVersion | 含 UsageCount/UsageBytes/ConcurrencyStamp | 【局部改】须补 ProviderKeyIdentity/MaterialVersion/StorageType/ProviderId/IntegrityValue |
| `:196-210` SysUser | Username/PasswordHash/LoginFailCount/LockedUntil，无手机号、无 MFA 字段 | 【局部改】须补 PhoneEncrypted/MFAEnabled/PasswordSalt/PasswordUpdatedAt/IntegrityValue/RoleId |
| `:238-258` SysAuditLog | 18 字段；无 ErrorCode | 【局部改】须补 ErrorCode |
| 33.3 Fluent API `:306-495` | 13 个 IEntityTypeConfiguration 完整配置 | 【可继承】配置风格与索引设计可沿用 |
| 33.4 DbContext `:497-528` | DbSet 14 个；ApplyConfigurationsFromAssembly | 【局部改】须补 11 个 DbSet |
| 33.5 DI `:530-563` | SnowflakeIdGenerator + UpdatedAtInterceptor + SnowflakeIdInterceptor + UseMySQL | 【可继承】雪花 ID 与拦截器机制可直接沿用 |
| 说明 `:563` | 雪花 ID 64 位结构（41+10+12），纪元 2020-01-01 | 【可继承】 |

### 6. `实现规范-HSM适配器.md`（第 37 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 37.1 项目结构 `:5-20` | `CryptoPlatform.Crypto/{Abstractions,Software,Hsm/Adapters}` 布局（与 5.6 章 13 项目结构不一致） | 【局部改】须统一项目结构 |
| 37.2 IHsmSdkAdapter `:22-66` | 13 个方法（连接、生成、加解密、签验、哈希、HMAC、随机、KeyInfo、Destroy、Health）；无 Wrap/Unwrap/Backup/Restore/Import/Export/RootKey/SelfTest/GetCapabilities | 【需重写】见 E 组 |
| 37.3 HSM DTO `:68-116` | HsmConnectionConfig、HsmKeyHandle、HsmKeyPairHandle、HsmKeyAttributes、HsmCryptoParams、HsmKeyInfo、HsmHealthInfo | 【局部改】须补身份校验字段（KCV/Fingerprint/ProviderKeyIdentity） |
| 37.4 连接池 `:118-196` | HsmConnectionPool（Channel + SemaphoreSlim + 健康回收） | 【可继承】实现质量高 |
| 37.5 错误码映射 `:198-224` | HsmErrorCodeMapper 字典映射 + 兜底 | 【可继承】 |
| 37.6 HsmCryptoProvider `:226-285` | 实现骨架；`GetCapabilities()` **硬编码 11 项能力字符串** | 【需重写】硬编码能力与实际设备能力脱节，须改为配置/协商 + 准入校验 |

### 7. `实现规范-KeyService.md`（第 34 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 34.1 DTO `:5-55` | CreateKeyCommand/RotateKeyCommand/DestroyKeyCommand/ImportKeyCommand/KeyDescriptor/KeyVersionDescriptor | 【局部改】CreateKeyCommand 应删 KeyUsage；DestroyKeyCommand 须补二次确认令牌 |
| 34.2 IKeyService `:57-75` | 10 方法（无 Backup/Restore/VerifyRestore/Expire） | 【局部改】须补 |
| 34.3 实现骨架 `:77-396` | Create（两阶段 + 补偿）、Activate、Rotate、Destroy、Disable/Revoke、查询、Import 骨架 | 【局部改】Destroy 为**单阶段**（与详细设计 §13.5 两阶段不一致，旧设计内部矛盾） |
| `:372-385` ValidateKeyTypeAndUsage | 允许组合含 `("ROOT","WRAP")` | 【需重写】须删 ROOT/WRAP，改为 KeyType 兼容性校验 |
| `:355-370` LoadKeyForUpdateAsync | 以 `Entry.State = Modified` 触发行锁 | 【局部改】须改为显式 `FOR UPDATE` 或等价机制（当前写法不产生行锁） |
| 34.4 DI `:398-409` | AddKeyService | 【可继承】 |
| 34.5 异常 `:411-427` | BusinessException + 11 个错误码字符串 | 【局部改】须映射到 V2.9 数值错误码 |

### 8. `实现规范-REST-API.md`（第 40 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 40.1 统一响应 `:5-38` | `ApiResponse<T>`（`int Code`）+ `ApiErrorResponse` | 【局部改】须映射 V2.9 数值分段（10000-90099） |
| 40.2 管理认证 Controller `:40-76` | `/api/v1/admin/auth/token` + `/token/revoke`；AdminLoginRequest(username,password) | 【需重写】路径/请求体缺 MFA、验证码、RefreshToken |
| 40.3 密钥管理 Controller `:78-173` | `/api/v1/admin/keys` 下 10 个端点（含 versions/import）；无 backup/restore/enable | 【局部改】须补 backup/restore/verify-recovery/enable；路径双入口须对齐 |
| 40.4 密码业务 Controller `:175-289` | `/api/v1/crypto/*` 10 个端点；无 algorithms/self-test | 【局部改】须补 2 个端点 |
| 40.5 全局异常处理 `:291-344` | BusinessException→400；CryptoProviderException→503；其他→500；不泄露内部信息 | 【可继承】 |
| 40.6 Program.cs `:346-382` | 服务注册、中间件顺序、Swagger（非 Scalar，与 5.5 章不一致） | 【局部改】须改为 Scalar/OpenAPI 并统一 |
| 40.7 端点汇总 `:384-408` | 21 个端点及认证方式标注（业务 API 标为 "AppSecret"） | 【需重写】须改为"请求直接签名/管理端 Token" |

### 9. `实现规范-审计与安全事件.md`（第 39 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 39.1 IAuditService `:5-31` | 接口 2 重载 + AuditLogEntry 14 字段 | 【局部改】须补 ErrorCode |
| 39.2 AuditService `:33-108` | 从 HttpContext 取上下文；计算哈希链后落库；更新最新哈希 | 【局部改】须改为 CanonicalSerialize + HMAC/SM3 规范 |
| 39.3 哈希链服务 `:110-179` | ComputeHash 用 `\|` 拼接 + **SHA256 占位**（注释"生产环境应使用 SM3"）；最新哈希存 Redis（sliding 30 天）；VerifyChain 区间校验 | 【需重写】须替换为 SM3 + 固定字段顺序 + `\0` 空值 + ISO8601；须去 Redis 单点，改分片链+聚合 |
| 39.4 安全事件服务 `:181-270` | RaiseAsync（自动判级）/ GetOpenEventsAsync / CloseAsync；DetermineSeverity 映射 6 类；CRITICAL 触发 SendAlert（TODO） | 【局部改】须补 CRITICAL 类型（NodeId 冲突、外部锚点失败、数据完整性、自检失败） |
| 39.5 后台任务 `:272-353` | SecurityDetectionWorker（1 分钟，仅注释占位）；KeyExpirationWorker（每分钟批量 EXPIRED） | 【局部改】须补完整性巡检 Worker、锚点生成 Worker、Nonce 清理 |
| 39.6 DI `:355-370` | AddAuditServices | 【可继承】 |

### 10. `实现规范-数据库DDL.md`（第 32 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 32.1 sys_application `:5-20` | app_id/app_name/status/ip_whitelist/description/created_at/updated_at | 【局部改】须补 IPWhitelistEnabled/AllowApiKeyCreate/IntegrityValue |
| 32.2 sys_application_secret `:22-39` | **仅 secret_hash** + secret_version + status + expires_at | 【需重写】须为 SecretCiphertext + SecretHash（V2.9 §7.3 :3205-3217） |
| 32.3 sys_key `:41-65` | key_type(SM2/SM4/HMAC/ROOT)、key_usage(SIGN/ENCRYPT/MAC/WRAP)、current_version、concurrency_stamp | 【需重写】须删 key_usage、删 ROOT；补 ProviderId/AlgorithmParams/MetadataIntegrityValue/RevokedAt/RotatedAt |
| 32.4 sys_key_version `:67-98` | 20 字段含 usage_count/usage_bytes/destroy_result/created_request_id | 【局部改】须补 ProviderId/StorageType/ProviderReference/ProviderKeyIdentity/MaterialVersion/EffectiveAt/IntegrityValue |
| 32.5 sys_key_authorization `:100-120` | key_id/app_id/permissions/status/expires_at/created_by/updated_by/revoked_at | 【可继承】 |
| 32.6 sys_crypto_device `:122-145` | device_id/device_name/vendor/model/serial_number/endpoint/provider_type/status/priority/is_primary/last_health_at/error_count | 【局部改】须补 device_type/supported_algorithms/provider_capabilities/master_slave_relation/cert_no/cert_authority/cert_expire_at/integrity_value |
| 32.7 sys_user `:147-164` | username/password_hash/display_name/status/login_fail_count/locked_until/last_login_at | 【局部改】须补 phone_encrypted/mfa_enabled/password_salt/password_updated_at/role_id/integrity_value |
| 32.8 sys_role/permission/user_role `:166-199` | role_code（5 角色）/permission_code/resource_type | 【局部改】须补 is_mutex_role、permission 的 role_id 口径 |
| 32.9 sys_audit_log `:203-230` | 22 字段含 previous_hash/current_hash；无 error_code | 【局部改】须补 error_code |
| 32.10 sys_security_event `:232-258` | 17 字段含 handling_result | 【局部改】须补锚点关联与 HandleResult 口径统一 |
| 32.11 sys_config `:260-273` | config_key/config_value/description/updated_by/updated_at | 【局部改】须补 config_scope/integrity_value |
| 32.12 sys_idempotency_record `:275-294` | app_id+method+path+key 唯一；response_code/response_body_hash；expires_at | 【可继承】 |
| 32.13 国产库适配 `:296-308` | 8 项适配说明 | 【可继承】 |

### 11. `实现规范-管理员密码认证与密码存储.md`

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 1.1-1.2 设计原则 `:8-31` | 管理员认证与密码服务分离；SM3≠密码存储 KDF；IPasswordHasher 可配置 KDF 架构 | 【可继承】抽象设计正确 |
| 2.1 默认算法 `:35-44` | **PBKDF2-HMAC-SHA256**，Salt 16B，迭代 600,000，子密钥 32B | 【需重写】V2.9 冻结 PBKDF2-SM3（§3.6.2 :1800、F-RK-009 :2120） |
| 2.2 合规备选 `:46-54` | PBKDF2-SM3，迭代 **100,000**，商密场景启用 | 【局部改】须升为默认；迭代与详细设计 1.6.3 的 600,000 冲突，须统一 |
| 2.3 选型说明 `:56-64` | 两算法对比表 | 【局部改】须按 V2.9 重排结论 |
| 3 哈希存储格式 `:67-93` | `{algorithm}${version}${base64salt}${iterations}${base64hash}` 自描述格式 | 【可继承】格式设计优秀，可保留（需与 V2.9 字段口径对齐） |
| 4.1 sys_user 密码字段 `:96-106` | password_hash/password_algorithm/password_version/password_changed_at/must_modify_pwd | 【局部改】V2.9 仅定义 PasswordHash/PasswordSalt/PasswordUpdatedAt，须二选一并冻结 |
| 5 升级策略 `:120-153` | 触发条件 4 条 + 透明升级流程 + 不回退原则 | 【可继承】 |
| 6.1 登录失败控制 `:159-165` | 5 次 / 锁 30 分钟 / 成功清零 | 【可继承】与 V2.9 §3.5.3 :1680 一致 |
| 6.2 安全事件上报 `:167-173` | ADMIN_LOGIN_FAILED / LOCKED / SUCCESS | 【可继承】 |
| 6.3 首次登录强制改密 `:175-180` | must_modify_pwd + MustModifyPassword 响应字段 | 【可继承】 |
| 7 接口设计 `:183-218` | IPasswordHasher / IPasswordHasherFactory / AdminLoginResponse | 【局部改】AdminLoginResponse 须补 MustBindMfa 等字段 |
| 8 合规对照 `:222-235` | 9 项等保/商密对照 | 【可继承】 |
| 9 后续扩展方向 `:238-248` | 7 项（含"MFA：登录链路已预留扩展点"、"Token 撤销增强：可扩展数据库持久化"） | 【需重写】MFA 已由"预留"变为**全局强制**；Token 撤销已变双写 |

### 12. `实现规范-认证授权.md`（第 38 章）

| 章节/行号 | 承载的设计内容 | 处置 |
| --- | --- | --- |
| 38.1 认证中间件 `:5-102` | 按路径分流：`/api/v1/admin` → Token；`/api/v1/crypto`、`/api/v1/keys` → AppSecret 签名 | 【需重写】须支持业务 API 双认证（签名或管理端 Token）+ 资源归属规则 |
| 38.2 IAdminTokenService `:104-212` | LoginAsync/ValidateAsync/RevokeAsync；JWT 签发；**BCrypt.VerifyPassword**；GetScopesForRole（5 角色→scope 数组） | 【需重写】须换 PBKDF2-SM3、加 MFA/验证码/AdminSession/RefreshToken；scope 须变 PERM_* 权限码 |
| 38.3 IAppAuthenticationService `:214-337` | 7 步校验；Timestamp 300s（±5min）；Nonce Redis 10 分钟；**旧签名串公式 `:285`**；`:287-290` 注释自认"用 secret_hash 无法验签"；失败 5 次锁定 1 小时 | 【需重写】公式、TTL、写序、存储模型全部冲突 |
| 38.4 密钥授权检查器 `:339-378` | KeyAuthorizationChecker：Owner 直通 + 显式授权 + permissions 含操作 | 【可继承】骨架可留 |
| 38.5 DI `:380-394` | AddAuthenticationServices | 【可继承】 |

---

## 第二部分　过时与冲突清单

> 冲突性质口径：**模型冲突**（数据/密钥/认证模型结论与 V2.9 不同）｜**字段缺失**（表/实体字段缺项）｜**流程变更**（步骤、顺序、阈值、时序变化）｜**接口变更**（API 路径/契约/接口签名变化）｜**已废弃**（V2.9 已删除或禁止的旧机制）｜**需新增**（旧设计完全未实现的能力）。

### A 组　业务 API 认证模型（旧：AppSecret/凭据签名 → 新：请求直接签名）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 1 | 详细设计文档.md §4.1.3 `:781-789`；核心模块详细设计与实现规范.md §6.3 `:873-874`；实现规范-认证授权.md §38.3 `:285` | `待签名内容 = HTTP Method + "\n" + Request Path + "\n" + Timestamp + "\n" + Nonce + "\n" + BodyHash（Base64 编码）`；`Signature = Base64(HMAC-SM3(appSecret, 待签名内容))` | 「stringToSign = HTTP-Method + CanonicalURI + CanonicalQueryString + CanonicalHeaders(去除末尾换行) + SignedHeaders + HexEncode(SM3(RequestBody))」（V2.9 §3.3.3.2 `:1271-1281`、§6.2.2 `:2609-2618`；F-AUTH-025/026 `:1412-1413`） | 接口变更 | 重写 |
| 2 | 详细设计文档.md §4.1.3 `:808`；实现规范-认证授权.md §38.3 `:230` | 「检查 Timestamp 偏差（默认允许 ±5 分钟）」；`_timestampToleranceSec = 300; // 5 分钟` | 「Timestamp 偏差不得超过 ±3 分钟；超时返回 10005」（F-AUTH-027 `:1414`；§6.10.3 `:2932`；§8.3.1 `:3743`） | 流程变更 | 修订 |
| 3 | 详细设计文档.md §4.1.2 `:762-766`；§4.4 `:896-898` | 「第三方系统无法进行登录交互，因此不要求 Token 认证」；「该接口仅限管理控制台调用，不对外暴露给第三方业务系统」 | 「管理端 Token 可访问管理 API 与业务 API（用于后台测试验证）」（F-AUTH-014 `:1491`）；「所有密码服务接口均支持两种认证方式……签名优先」（§6.5 `:2759`） | 流程变更 | 修订 |
| 4 | 详细设计文档.md §4.1.1 `:726-742`；实现规范-REST-API.md §40.3 `:78-173` | 管理端 API 与管理端密钥接口均无"调用业务 API 时资源归属"概念；仅要求 Token 含 `scope` | 「管理端 Token 无 AppId，调用涉及 KeyId 的业务 API 时必须显式传 `appId` 参数」；`ActingAsAppId` 须入审计；「不得成为绕过密钥隔离的后门」（§6.5 `:2784-2801`、§8.4 `:3803`） | 需新增 | 新增 |
| 5 | 实现规范-REST-API.md §40.7 `:399-408` | 端点汇总表"认证方式"列对 `/api/v1/crypto/*` 统一标注为 `AppSecret` | 业务 API 认证方式为「请求直接签名（HMAC-SM3）」，AppSecret 不在网络传输（§6.1 `:2586`、§8.3.1 `:3736-3744`） | 接口变更 | 修订 |
| 6 | 详细设计文档.md §4.1.3 `:768-821`；实现规范-认证授权.md §38.3 | 无 CanonicalURI/CanonicalQueryString/CanonicalHeaders/SignedHeaders 规范化规则，无 RFC 3986 编码、无 Header 小写排序、无 `%20` 规则、无空 Body 的 SM3 固定值、无固定测试向量 | 「Canonical Request 跨语言实现规范」7 条 + 固定测试向量 + 空 Body SM3 值 `1ab21d83…`（§3.3.3.3 `:1283-1364`；§6.2.3 `:2621-2672`） | 需新增 | 新增 |

### B 组　AppSecret 存储与生命周期（旧：明文/简单 Hash → 新：SecretCiphertext + SecretHash）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 7 | 详细设计文档.md §3.2.2 `:549-563`；§15.4 `:2153-2168`；实现规范-数据库DDL.md §32.2 `:22-39`；实现规范-EFCore实体与配置.md `:104-115` | `sys_application_secret` 仅 `secret_hash VARCHAR(255)` + `secret_version` + `status` + `expires_at`；「AppSecret 只存哈希，不提供找回功能」 | 「ApplicationSecret（含 SecretCiphertext + SecretHash）」；字段为 `SecretCiphertext`（SM4-GCM，KEK-Runtime 保护）+ `SecretHash`（SM3）（§7.1 `:3108`、§7.3 `:3205-3217`；§1.5 `:252-254`） | 字段缺失 | 修订 |
| 8 | 详细设计文档.md §5.3 `:1082-1092` | 「AppSecret 采用不可逆哈希存储：`secret → KDF/Password Hash → secret_hash`」「数据库不得保存原文」 | 「AppSecret 原文**不得持久化保存**；服务器端以受 **KEK-Runtime 保护的密文**形式保存，并仅在请求签名验证的短生命周期内解密使用」（§3.3.2 `:1205-1212`、§8.17 `:3950-3955`；NF-CMP-017 `:2397`） | 模型冲突 | 重写 |
| 9 | 核心模块详细设计与实现规范.md §6.3 `:880`；实现规范-认证授权.md §38.3 `:287-290` | 「用 secret_hash 校验签名（**PBKDF2 还原** → HMAC-SM3 计算 → 常量时间比较）」；代码注释「使用存储的 secret_hash 无法直接验签 → 需要存储明文 secret 的加密版本……此处为设计骨架」 | 「4. 解密 SecretCiphertext 获取 AppSecret（或从受限内存短缓存获取）」（§3.3.3.4 `:1379`、§6.2.4 `:2683`；F-AUTH-029 `:1416`） | 模型冲突 | 重写 |
| 10 | 详细设计文档.md §4.1.3 `:821`；§5.3 `:1092`；§15.4 `:2157-2168` | 「轮换期间旧版本保持短暂有效（默认 24 小时），保证第三方系统平滑切换」；「Secret V1 ACTIVE / Secret V2 CREATED → Secret V2 ACTIVE / Secret V1 REVOKED」 | 「**AppSecret 轮换后，旧 AppSecret 立即失效**（无兼容期）」；轮换为高风险操作须强制二次确认（§3.3.2 `:1219-1220`；§6.13.2 `:3011-3014`） | 流程变更 | 删除 |
| 11 | 详细设计文档.md §5.3 `:1082-1092`；§4.1.3 `:816-821` | 无解密后短缓存设计，仅要求"不可找回"、"不保存原文" | 「允许在安全内存中短缓存解密后的 AppSecret，TTL 建议 ≤60 秒（Software）/ ≤30 秒（HSM）」；须仅进程内存、禁止序列化、轮换/撤销立即清除、受保护内存、可清零（§3.3.2 `:1224-1249`；F-AUTH-030 `:1417`；§8.17 `:3957-3960`） | 需新增 | 新增 |
| 12 | 详细设计文档.md §4.1.3 `:819-820`；实现规范-认证授权.md §38.3 `:231`,`:304-314` | 「appSecret 连续认证失败达到阈值（默认 5 次 / 10 分钟）时，锁定该应用并产生安全事件」「锁定后需管理员通过管理控制台手动解锁」；`app:locked:{appId}` 锁 1 小时 | 「应用签名连续失败（同一 AppId + 来源 IP）10 次 / 5 分钟 → 告警 + 对该 AppId+IP 组合**限流，不自动锁定应用**」；「默认不自动锁定应用」；禁用须安全管理员人工操作（§3.5.3 `:1678`、`:1698-1704`） | 流程变更 | 重写 |

### C 组　完整性校验（旧：无/简单 Checksum → 新：IntegrityValue + IntegrityKey）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 13 | 详细设计文档.md §3 全章 `:511-706`、§15 `:2080-2170`、§16 DDL `:2176-2238`；实现规范-数据库DDL.md §32 全章 `:5-294`；实现规范-EFCore实体与配置.md `:87-304` | 全部核心表（sys_key、sys_key_version、sys_application、sys_application_secret、sys_user、sys_role、sys_config 等）**均无完整性保护字段**；检索确认 `IntegrityValue`/`Checksum` 在旧设计中**零出现** | 「关键数据表必须增加 IntegrityValue 字段」；7.17.1 列出 20 张纳入校验的表与逐表关键字段（§7.17.1 `:3620-3646`、§7.17.2 `:3648-3650`；NF-DATA-008 `:2354`；F-DI-001～003 `:1129-1131`） | 字段缺失 | 新增 |
| 14 | 详细设计文档.md §3.1 `:513-532`；§30 `:3189-3247` | 数据库设计原则仅列"不得保存"清单；密钥包装体系仅 Root Key→KEK→DataKey，**无 IntegrityKey 概念** | 「算法：HMAC-SM3(IntegrityKey, CanonicalSerialize(...) + TableName + RowId)」；「IntegrityKey 由 KEK-Runtime 保护」；极高价值数据可 SM2 签名加强（§7.11.2 `:3534-3537`；NF-CMP-016 `:2396`；§1.5 `:266-267`） | 需新增 | 新增 |
| 15 | 详细设计文档.md §6.4 `:1230-1248` | 「`Log[n] → SM3(Log[n] + Hash[n-1]) → Hash[n]`」；「每个审计分区维护前置哈希」；「更高安全等级下，对检查点执行数字签名并存储到独立介质」 | 「审计哈希链必须使用统一的 CanonicalSerialize」；H1=SM3(CanonicalSerialize(Log1))、Hn=SM3(CanonicalSerialize(Logn)+Hn-1)；「定期 SM2 Sign(Hn)」；字段顺序/空值 `\0`/ISO8601/UTF-8/`\|` 转义规则固定（§3.4.4 `:1579-1604`） | 模型冲突 | 重写 |
| 16 | 实现规范-审计与安全事件.md §39.3 `:134-150` | `ComputeHash` 用 `\|` 拼接 9 个字段；`System.Security.Cryptography.SHA256.HashData(bytes); // 生产环境应使用 SM3`；最新哈希存 Redis `audit:chain:latest_hash`（Sliding 30 天） | CanonicalSerialize 字段固定 16 项、编码 UTF-8、分隔符 `\|`、空值占位、时间 ISO8601 带时区；须多实例分片链 + 定期聚合签名（§3.4.4 `:1584-1617`） | 模型冲突 | 重写 |
| 17 | 详细设计文档.md §6.4 `:1244-1248`；§39 未涉及 | 仅"周期性生成检查点 CheckpointHash"，"更高安全等级下"可选签名；**无完整性巡检记录实体与巡检策略** | 新增 `IntegrityScanRecord` 实体（ScanId/StartedAt/FinishedAt/ScanScope/TotalRecords/ViolationCount/Result/Operator/Detail）；「定期巡检采用分批、限速、低峰执行，默认每 24 小时一轮」（§7.1 `:3128`、§7.10 `:3504-3518`、§7.17.4 `:3679-3687`） | 需新增 | 新增 |

### D 组　密钥体系（旧：单层/两层 → 新：L0～L3 四层 + Provider 物理实现互斥）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 18 | 详细设计文档.md §1.6.2 `:166-172`；§1.6.3 `:215-229`；§30.1 `:3189-3221` | 「密钥材料必须使用平台密钥加密体系保护：`DataKey → Key Encryption Key → Root Key`」；无 HSM 视图为 `Master Password→Protection Key→Keystore File→Root Key→KEK→DataKey`（**混合 3～5 层，无 Unlock KEK / Runtime KEK 命名**） | 「L0 Unlock KEK（KEK-Unlock）→ 解封 → L1 Root Key → 保护 → L2 Runtime KEK（KEK-Runtime）→ 保护 → L3 Data Key」；四层职责表（§3.11.1 `:1949-1966`；F-RK-002/F-KEK-002 `:2113`、`:2126`） | 模型冲突 | 重写 |
| 19 | 详细设计文档.md §1.6.3 `:174-240` | Keystore 文件（PKCS#12 或自定义）+ 主密码（PBKDF2-HMAC-SM3 ≥600,000 次）+ SM4-GCM 加密；文件权限 chmod 600；主密码不落盘；失败 5 次拒绝 | 部分一致：「管理员主密码 → KDF → KEK-Unlock → Unwrap → Root Key → Wrap → KEK-Runtime → Wrap → Data Key」（§3.11.2 `:1976-1980`）；F-RK-008/009/010 `:2119-2121`；PBKDF2-SM3 参数须在详细设计冻结 | 模型冲突 | 修订 |
| 20 | 详细设计文档.md §1.6.3 `:233-238` | 「部署安全等级配置」表：生产+HSM=推荐；生产+无 HSM=需安全评审批准；**开发/测试=配置文件明文（仅限非生产）**；生产+无 HSM+无主密码=禁止 | 无此分级。V2.9：部署模式仅 Software 或 HSM 二选一（§5.6.3 `:2531-2533`、§3.11.2 `:2008`）；软件密码模块允许用于生产（§5.6.2 `:2512`）；「不得以'默认安全'替代实际密码设备要求」（旧设计同句已不适用） | 已废弃 | 删除 |
| 21 | 详细设计文档.md §2.6 `:481-490`；§2.3 `:431`；§36.1 `:3516` | 「SoftwareCryptoProvider 用于：开发环境；测试环境；**HSM 不可用时的明确降级场景（如安全策略允许）**」；「不允许未经策略配置自动把 HSM 密钥转换成软件密钥」 | 「**部署模式互斥**：平台仅支持 Software 或 HSM 一种部署模式，**不支持混合部署**」；「切换部署模式需完整迁移密钥体系，需安全管理员审批」（§5.6.3 `:2531-2533`、§3.11.2 `:2008`、§2.6 `:625`） | 已废弃 | 删除 |
| 22 | 详细设计文档.md §1.3 `:83-92`；§15.1 `:2082-2101`；§16 `:2177-2196`；实现规范-数据库DDL.md §32.3 `:48-49`；实现规范-EFCore实体与配置.md `:17-31` | `sys_key.key_type` 取值 `SM2/SM4/HMAC/**ROOT**`，`key_usage` 取值 `SIGN/ENCRYPT/MAC/**WRAP**`；「ROOT 平台密钥加密体系；根密钥，仅允许安全设备保护」 | 「KeyType，取值：**SM2 / SM4 / HMAC**」（§1.5 `:249`）；Root Key 独立为 `RootKeyMetadata` 实体（RootKeyId/ProviderId/Status/KcvReference/…）（§7.10 `:3478-3489`；F-RK-001 `:2112`） | 模型冲突 | 重写 |
| 23 | 详细设计文档.md §1.4 `:94-111`；§28 表 `:2088`；实现规范-数据库DDL.md §32.3 `:49` | `key_usage VARCHAR(32)` 为必填字段，创建后不可修改；`SIGN/ENCRYPT/MAC/WRAP` | 「**V2.6 删除 KeyUsage 字段**（平台不约束业务语义）；新增 KeyMaterial 实体承载密钥材料」（§7.2 `:3151`）；Key 数据字段表**无 KeyUsage**（§7.2 `:3132-3151`） | 模型冲突 | 删除 |
| 24 | 详细设计文档.md §1.2.2 `:60-79`、§15.2 `:2103-2129`、§25.3 `:2732-2763`；实现规范-数据库DDL.md §32.4 `:70-98` | `sys_key_version` 承载 `provider_key_ref` / `encrypted_key_material` / `fingerprint`；**无独立 KeyMaterial 实体，无 ProviderKeyIdentity、无 MaterialVersion、无 StorageType** | 「Key → KeyVersion → **KeyMaterial**（ProviderId、StorageType（WRAPPED_DATABASE/HSM_REFERENCE）、WrappedMaterial、ProviderReference、**ProviderKeyIdentity**、MaterialVersion、IntegrityValue）」（§3.11.4 `:2077-2090`、§7.2 `:3167-3187`）；「ProviderKeyIdentity 必须参与 IntegrityValue 计算」（`:3187`） | 字段缺失 | 新增 |
| 25 | 实现规范-数据库DDL.md §32 全章；核心模块 §1.3 `:85-100` | 核心表共 **14 张**，无 RootKeyMetadata / KekMetadata / BackupRecord / AlertRule / UserDataDEK 等 | 核心数据实体至少 **22 个**，含 `RootKeyMetadata`、`KekMetadata`（KEK-Runtime/KEK-Backup）、`UserDataDEK`、`BackupRecord`、`AlertRule`、`IntegrityScanRecord`、`SignatureNonce`、`AdminSession`、`RefreshToken`、`UserMfaBinding`、`KeyMaterial`（§7.1 `:3103-3128`；§7.10 `:3451-3502`） | 字段缺失 | 新增 |
| 26 | 详细设计文档.md §1.1 `:20-35`；§1.8 `:288-315`；§30 `:3189-3247` | Root Key 仅作为"ROOT 类型密钥"参与 Key 生命周期；轮换/备份一律执行，**无 Provider 能力门槛，无 KCV/Wrap-Unwrap 校验** | 「HSM 场景仅在 Provider 支持 **CanRotateRootKey / CanRewrapKey** 时执行，需安全管理员审批、二次确认」（F-RK-004 `:2115`）；「Root Key 完整性/**KCV** 验证 → 使用测试 KEK 执行 Wrap/Unwrap 验证」（§3.11.3 `:2021-2023`）；KCV 优先级回退规则（`:2045`） | 需新增 | 新增 |
| 27 | 详细设计文档.md §1.7 `:242-286`；§29.1 `:3175-3187` | 只有**一套**状态机（Key 与 KeyVersion 共用状态常量）；状态-操作矩阵按状态给值，无 KeyVersion 独立状态语义 | 「**KeyVersion 状态机**（V2.9 补充）：KeyVersion 拥有独立状态，与 Key 状态协同但不完全等同」+ 14 条 ST-KV 规则（§3.2.3 `:901-934`） | 需新增 | 新增 |

### E 组　Provider 能力模型（旧：强制统一接口 → 新：Capability 最小准入集 + 可选扩展接口 + NotSupported）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 28 | 详细设计文档.md §2.2 `:377-413`、§14.2 `:1956-2010`、§28.2 `:3060-3117`；核心模块 §4.2 `:581-620`；实现规范-CryptoProvider接口.md §36.1 `:5-43`；实现规范-HSM适配器.md §37.2 `:22-66` | 所有密码设备必须实现**同一套强制接口**（生成、加解密、签验、哈希、HMAC、随机、KeyInfo、Destroy、Health 各 12～13 个方法）；接口内注释「实际项目中不建议让业务层直接传递密钥字节」 | 核心接口 + **可选扩展接口族**：「非所有密码设备都必须实现以下扩展能力；由 ProviderCapabilities 与可选接口共同决定」——`IKeyWrappingProvider` / `IKeyBackupProvider` / `IKeyRestoreProvider` / `IKeyImportProvider` / `IKeyExportProvider` / `IRootKeyProvider`（§2.6 `:561-581`） | 接口变更 | 重写 |
| 29 | 详细设计文档.md §14.4 `:2031-2059`；核心模块 §4.2 `:618`；实现规范-CryptoProvider接口.md §36.1 `:41`；实现规范-HSM适配器.md §37.6 `:277-279` | `IReadOnlySet<string> GetCapabilities()` 返回字符串集合；`HsmCryptoProvider.GetCapabilities()` **硬编码 11 项能力**；"API 层在执行前检查 Capability" | `ProviderCapabilities` 至少 10 项布尔能力（CanWrapKey/CanUnwrapKey/CanBackupKey/CanRestoreKey/CanImportKey/CanExportKey/SupportsNonExportableKey/CanRotateRootKey/CanRewrapKey/SupportsKeyIdentityVerification）；「**最小能力准入集**：CanWrapKey/CanUnwrapKey 为强制准入项，接入时必须声明并实测通过」；「不支持的能力必须返回明确的 `NotSupported` 结果，并记录审计」（§2.6 `:585-610`；§3.11.5 `:2105`；§6.6 `:2846-2850`） | 接口变更 | 重写 |
| 30 | 详细设计文档.md §2.2 `:377-413`；实现规范-CryptoProvider接口.md §36.1 `:5-43`；实现规范-HSM适配器.md §37.2 `:22-66` | 接口中**无** Wrap/Unwrap、Backup/Restore、Import/Export、RootKey 生成/轮换/重包裹方法 | `IKeyWrappingProvider{ WrapKeyAsync, UnwrapKeyAsync }`；`IKeyBackupProvider`/`IKeyRestoreProvider`/`IKeyImportProvider`/`IKeyExportProvider`/`IRootKeyProvider`（§2.6 `:561-581`、`:612-618`） | 接口变更 | 新增 |
| 31 | 详细设计文档.md §2.2 `:377-413`；核心模块 §4.2 `:581-620`；实现规范-CryptoProvider接口.md §36.1 `:5-43` | 接口中**无自检方法**（无 `SelfTestAsync`）；全文无 KAT 概念 | `Task<SelfTestResult> SelfTestAsync(...)`（§2.6 `:558`）；「F-KAT-001 上电自检」「F-KAT-002 周期自检（默认 24 小时）」「F-KAT-003 按需自检」「自检失败时拒绝提供密码运算并告警」（§3.1.6 `:800-814`） | 需新增 | 新增 |
| 32 | 详细设计文档.md §13.5.2 `:1883-1895`；核心模块 §2.2.7 `:304-307` | Provider 销毁结果枚举 `Success/NotFound/NotSupported/Failed/Timeout`；"NotFound 不能直接等同于成功销毁" | 「`DestroyKeyAsync` 必须返回明确的 `DestroyKeyResult`，包含：是否成功、**在线材料是否已销毁、归档材料保留标记**、失败原因」；DESTROYED 定义为"在线运行密钥材料已完成密码学销毁"，ARCHIVE MATERIAL 另计（§2.6 `:583`、§3.2.3 `:898-899`） | 接口变更 | 修订 |
| 33 | 详细设计文档.md §2.2 `:384`；核心模块 §4.2 `:588`；实现规范-CryptoProvider接口.md §36.1 `:12` | `string ProviderType { get; } // "HSM" / "SOFTWARE"`；`string ProviderName` | `string ProviderId { get; }`；`KeyStorageMode StorageMode { get; } // Software / Hsm`；`ProviderCapabilities Capabilities { get; }`（§2.6 `:542-547`）；平台数据模型保存 `ProviderId / ProviderReference / ProviderKeyIdentity`（§3.11.2 `:2006`） | 接口变更 | 修订 |
| 34 | 实现规范-CryptoProvider接口.md §36.6 `:192-215`；实现规范-HSM适配器.md `:254` | `GetDefaultProvider() { return _providers.First(); }`（硬编码首个 Provider）；`ResolveProvider(version)` 仅按 `ProviderType` 匹配；`DeviceId = null // 由 Router 填充` | 路由须结合 `ProviderId`、`StorageMode`、`ProviderCapabilities` 与设备注册信息解析；准入校验在注册与启用流程强制执行（§2.6 `:610`、§3.11.2 `:1996-2006`、§6.13.3 `:3027`） | 接口变更 | 修订 |
| 35 | 详细设计文档.md §1.6.1 `:146-160`；实现规范-HSM适配器.md §37.3 `:103-108` | HSM 模式数据库仅存 `ProviderType/DeviceId/ProviderKeyRef` + fingerprint + metadata；`HsmKeyInfo` 仅 `Exists/Algorithm/CreatedAt` | 须保存并校验 `ProviderKeyIdentity`（KCV / PublicKeyFingerprint / 厂商等价机制）：「恢复后必须通过 `Platform KeyId → ProviderReference → ProviderKeyIdentity` 建立确定性映射」；「无法提供任何可接受的密钥身份/完整性验证能力，则不得将 Provider 标记为 READY」（§3.11.3 `:2030-2045`、§3.11.5 `:2103`、§7.2 `:3178`） | 需新增 | 新增 |

### F 组　管理端认证（旧：JWT Token → 新：Access Token + AdminSession + RefreshToken 轮换 + MFA 全局强制）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 36 | 详细设计文档.md §5.4 `:1094-1118`、§5.5 `:1120-1133`；实现规范-认证授权.md §38.2 `:104-190` | JWT 无状态校验 10 步（签名/exp/nbf/iss/aud/operatorId/状态/scope/KeyAuth/时间戳与 Nonce）；**无服务端会话状态**，仅 Redis revoked jti | 「**AdminSession 实体**：SessionId/UserId/Jti/IssuedAt/**LastAccessAt**/AbsoluteExpireAt/RevokedAt/Status」；「JWT 无状态，**无法单独实现"空闲超时"，需配合服务端会话状态**」；校验流程 6 步（§3.3.4 `:1432-1463`、§7.5 `:3313-3327`；F-AUTH-012/016 `:1489`、`:1493`） | 模型冲突 | 新增 |
| 37 | 详细设计文档.md §5.4/§5.7 `:1094-1165`；实现规范-认证授权.md §38.2 `:104-196` | 仅签发 `accessToken`；无 Refresh Token、无 RefreshKey | `Refresh Token：随机 opaque token（非 JWT）、长度 ≥256 bit、仅存 Hash（HMAC-SM3 with RefreshKey）、**一次性使用（rotation）**、绝对 7 天、绑定 UserId+DeviceId`；`RefreshKey 由 KEK-Runtime 保护，支持轮换`（§3.3.4 `:1465-1483`、§7.5 `:3329-3352`；F-AUTH-011/017 `:1488`、`:1494`） | 需新增 | 新增 |
| 38 | 实现规范-管理员密码认证与密码存储.md §9 `:245` | 「**MFA（多因素认证）**：登录链路已预留扩展点」；旧设计其余文档 MFA **零出现**（全局检索确认） | 「管理端 MFA **全局强制**（`MFA_POLICY_MODE` 默认 `REQUIRED`，**生产环境冻结为 `REQUIRED`**）」；「口令校验通过后**必须完成 MFA 第二因素验证，方可获得会话**」；F-CON-013 全局强制 MFA（§3.3.4 `:1421-1423`、§3.6.3 `:1804-1838`、§8.3.2 `:3751`、§8.5 `:3809-3815`；F-CON-009～013 `:1784-1788`） | 需新增 | 新增 |
| 39 | 实现规范-数据库DDL.md §32.7 `:147-164`；实现规范-EFCore实体与配置.md `:196-210` | `sys_user` 字段：username/password_hash/display_name/status/login_fail_count/locked_until/last_login_at；**无 MFA 相关字段、无手机号** | 新增 `UserMfaBinding` 实体：BindingId/UserId/MfaType（TOTP/USBKey/SM2Cert/Token）/CredentialHash/PublicKey/DeviceLabel/Status（PENDING/ACTIVE/REVOKED）/BoundAt/LastUsedAt/ResetAt/IntegrityValue（§7.4 `:3247-3261`；F-DI-007 `:1858`） | 字段缺失 | 新增 |
| 40 | 详细设计文档.md §4.4 `:865-898`；实现规范-REST-API.md §40.2 `:54-72` | `POST /api/v1/admin/auth/token`（请求体仅 `{username, password}`）+ `POST /api/v1/admin/auth/token/revoke` | `POST /api/v1/admin/auth/login`（用户名+口令+验证码/风险控制+MFA）、`POST /api/v1/admin/auth/logout`（同时撤销 Access Token 与 Refresh Token）、`POST /api/v1/admin/auth/token/refresh`（使用 Refresh Token）（§6.7 `:2852-2860`） | 接口变更 | 重写 |
| 41 | 详细设计文档.md §4.4 `:888`；§5.1 `:1060` | 「Token 有效期 2 小时（可配置）」；响应 `"expiresIn": 7200` | 「**空闲超时**：15 分钟无操作后失效；**绝对超时**：24 小时后强制失效」（§3.3.4 `:1428-1429`、§6.3 `:2723-2724`、§8.3.2 `:3753`） | 流程变更 | 重写 |
| 42 | 详细设计文档.md §5.7 `:1151-1165`；实现规范-认证授权.md §38.2 `:186-196` | Token 撤销仅写 Redis：`token:revoked:{jti}`，TTL 不超过 Token 剩余有效期 | 「撤销列表存储于**缓存与数据库双写，缓存失效时回退数据库**」（§6.3 `:2726`；§3.3.4 `:1490`）；AdminSession 含 `RevokedAt/Status` 字段 | 流程变更 | 修订 |
| 43 | 详细设计文档.md §5.4 `:1118` | 「签名算法由平台安全配置统一确定」 | 「JWT 格式，**签名算法 SM2**」（§3.3.4 `:1427`、§6.3 `:2722`、§8.6 `:3821`） | 接口变更 | 修订 |
| 44 | 详细设计文档.md §3.2.6 `:615-627`；实现规范-数据库DDL.md §32.8 `:166-199`；实现规范-认证授权.md §38.2 `:201-208` | 建议角色 5 个（SYSTEM_ADMIN/KEY_ADMIN/APP_ADMIN/SECURITY_AUDITOR/OPS_ADMIN），**无互斥约束**；权限以 JWT `scope` 表达（`key:manage`/`app:manage`/`audit:read`）；`sys_permission` 字段为 permission_code/permission_name/resource_type | 「平台采用**等保三员体系，三员强制互斥**」：系统管理员/安全管理员/审计管理员 + 2 扩展角色；`Permission 权限矩阵` 以 `PERM_*` 编码表达 19 项权限；权限检查顺序含「**三员互斥校验**」；Permission 实体字段为 `PermissionId、RoleId、PermissionCode`（§3.6.1 `:1734-1770`、§8.4 `:3779-3795`、§7.17.1 `:3632`） | 模型冲突 | 新增 |
| 45 | 详细设计文档.md §4.4 `:894-898`；实现规范-认证授权.md §38.2 `:137-157` | 登录流程为「用户名 + 密码」两步；仅"连续登录失败达到阈值时锁定账号" | 「用户名 + 口令 + **验证码/风险控制** + MFA 第二因素验证」（§6.7 `:2858`、F-AUTH-010 `:1487`） | 需新增 | 新增 |
| 46 | 详细设计文档.md §5.8 `:1172-1180` | 敏感操作二次确认 **5 项**：密钥销毁、AppSecret 重置、用户权限变更、HSM 配置、Root Key 操作 | 高风险操作二次确认 **15 项**：密钥销毁、密钥恢复、密钥导入、AppSecret 重置/轮换/撤销、业务 API 创建密钥（如开启）、Root Key 相关、管理员角色变更、安全策略修改、KEK-Backup 轮换/销毁、HSM 认证证书禁用、用户 MFA 重置/解绑、用户手机号变更、MFA 策略切换（§3.6.4 `:1840-1849`） | 流程变更 | 修订 |

### G 组　手机号与用户数据（旧：无 / 未实现 PhoneHash → 新：普通属性 + UserDataDEK，删 PhoneHash，新增 UserMfaBinding）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 47 | 实现规范-数据库DDL.md §32.7 `:147-164`；实现规范-EFCore实体与配置.md `:196-210`；详细设计文档.md §3.2.6 `:615-627` | `sys_user` / `SysUser` **无手机号字段**；用户模型仅用户名/密码哈希/显示名/状态/登录统计 | User 实体须含 `PhoneEncrypted`（SM4-GCM + UserDataDEK 加密）、`MFAEnabled`、`PasswordSalt`、`PasswordUpdatedAt`、`RoleId`、`IntegrityValue`；「手机号为用户表的**普通属性字段**」（§7.4 `:3230-3245`、`:3263-3274`；§8.16 `:3929-3944`） | 字段缺失 | 新增 |
| 48 | 详细设计文档.md §1.6 `:144-240`；§30 `:3189-3247`；实现规范-数据库DDL.md §32 全章 | 密钥保护层级中**无 UserDataDEK / DEK 概念**，无针对用户敏感字段的独立数据密钥 | `UserDataDEK` 实体：DekId/Purpose（如 PHONE_ENCRYPTION）/Status/WrappedMaterial（KEK-Runtime 保护）/CreatedAt/RotatedAt/IntegrityValue；含 8 步轮换流程（双 Key 窗口、断点续传、限速低峰、安全管理员权限）（§7.4 `:3288-3311`；§7.1 `:3125`） | 需新增 | 新增 |
| 49 | 详细设计文档.md 全文；核心模块 §1.2 `:66-77`；实现规范-数据库DDL.md §32 全章 | 全局检索确认：`PhoneHash`、`PhoneSearchKey` 在旧设计中 **零出现**（旧设计未实现该机制） | 「V2.6 引入的 PhoneSearchKey / PhoneHash（HMAC-SM3 密钥化搜索）机制自 V2.8 起**删除**」；「不建立独立搜索密钥，不做去重/唯一性特殊处理」（§7.4 `:3286`、§8.16 `:3941`） | 已废弃 | 无需处置（确认无残留，仅需在变更说明中显式声明） |

### H 组　防重放（旧：简单 Nonce → 新：缓存+数据库双写、UNIQUE(AppId,Nonce) 最终仲裁、TTL 6 分钟）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 50 | 详细设计文档.md §4.1.3 `:809`；§7.2 `:1310-1313`；实现规范-认证授权.md §38.3 `:270-274` | 「检查 Nonce 是否已使用（Redis 防重放，**默认有效期 10 分钟**）」；`nonceKey = $"nonce:{appId}:{nonce}"`；`AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)`；**仅 Redis，无数据库回退** | 「Nonce（X-Nonce）：TTL 不小于时间窗口跨度的 2 倍（**默认 6 分钟**）」；「主存储：缓存；**回退存储：数据库短期存储**」；`SignatureNonce` 实体（NonceId/AppId/Nonce/Timestamp/CreatedAt/ExpireAt）（§6.10.3 `:2933`、`:2936-2943`、§7.6 `:3354-3374`） | 流程变更 | 重写 |
| 51 | 实现规范-认证授权.md §38.3 `:267-274` | 顺序为：**先写 Nonce**（`SetStringAsync`）→ 再查 Secret → 再验签（`expectedSig` 比对） | `V2.5 的 DoS 风险：先写入 Nonce 再验证 HMAC，攻击者可用大量无效 Nonce 灌满缓存`；修正为「6. 比对签名：不一致 → 10004（记录安全事件，**不写入 Nonce**）→ 7. HMAC 成功后原子写入 Nonce」（§3.3.3.4 `:1366-1398`、§6.2.4 `:2674-2702`；F-AUTH-028 `:1415`） | 流程变更 | 重写 |
| 52 | 详细设计文档.md §4.1.3 `:809`；实现规范-认证授权.md §38.3 `:270-274` | 无数据库 UNIQUE 约束，无仲裁规则；仅 Redis `exists` 判断（存在 `SELECT → INSERT` 类竞态） | 「数据库侧使用 **`UNIQUE(AppId, Nonce)` 约束，禁止 SELECT → INSERT**」；「Nonce 双写一致性以数据库 UNIQUE 约束为**最终仲裁**」；「禁止将缓存结果作为最终仲裁」（§6.10.3 `:2941-2943`、§6.2.4 `:2700-2701`、§3.3.3.4 `:1396-1397`） | 需新增 | 新增 |
| 53 | 详细设计文档.md §3 数据库详细设计 `:511-706`；实现规范-数据库DDL.md §32 | **无 SignatureNonce 表**；请求签名 Nonce 仅存在于 Redis 键空间 | 核心实体 10 项 `SignatureNonce`；「不纳入完整性值校验：SignatureNonce（短生命周期）」；保留「TTL 后清理」（§7.1 `:3116`、§7.6 `:3354-3374`、§7.11.2 `:3563`） | 字段缺失 | 新增 |

### I 组　审计（旧：简单哈希链 → 新：CanonicalSerialize + 分片聚合 + SM2 签名 + 外部锚点 + 集中外发）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 54 | 详细设计文档.md §6.4 `:1244-1248`；§6.5 `:1250-1265` | 「周期性生成检查点：`CheckpointHash`」「**更高安全等级下**，对检查点执行数字签名并存储到独立介质」——签名与外部存储为**可选**，无锚点实体 | 「**外部锚点（V2.6 补充）**：为防止数据库管理员删除最后 N 条日志后重新构造哈希链，平台**必须**建立外部锚点」；「校验点使用 SM2 签名，签名私钥由 Root Key 保护」；「校验点外发至集中审计平台或独立存储（**与平台数据库物理隔离**）」；「恢复审计链时需同时校验内部链与外部锚点」；`AnchorId/StartAuditId/EndAuditId/ChainHeadHash/GeneratedAt/Signature/ExternalRef`（§3.4.4 `:1619-1642`、§7.7 `:3384-3394`；§8.8 `:3831`） | 需新增 | 新增 |
| 55 | 详细设计文档.md §6.4 `:1242` | 「每个审计分区维护前置哈希」——仅一句，无分片规则、无聚合签名 | 「分片链 + 校验点：按应用/租户分片写入各自哈希链，定期对分片链头进行**聚合签名**，形成全局校验点」；「必须保证任一分片链可独立验证」；「分片规则（按 AppId / 租户 / 固定分片数）由详细设计确定，但必须保证同一审计事件的归属分片稳定」（§3.4.4 `:1606-1617`） | 需新增 | 新增 |
| 56 | 详细设计文档.md §6 全章 `:1184-1287`；实现规范-审计与安全事件.md §39 全章 `:1-370` | 无集中审计外发通道设计（`SendAlertAsync` 仅为 TODO 占位） | 「集中审计外发时，外发通道必须加密，外发数据应带审计链校验点；**外发失败应本地留存并支持断点续传，不得丢弃审计事件**」（§3.4.4 `:1614-1615`；§8.8 `:3831`） | 需新增 | 新增 |
| 57 | 实现规范-数据库DDL.md §32.9 `:203-230`；实现规范-EFCore实体与配置.md `:238-258`；实现规范-审计与安全事件.md §39.1 `:16-30` | `sys_audit_log` 22 字段：id/audit_id/request_id/trace_id/timestamp/operator_type/operator_id/operator_name/app_id/source_ip/operation/key_id/key_version/result_code/duration_ms/previous_hash/current_hash/created_at；**无 error_code** | CanonicalSerialize 字段序列含「Result + **ErrorCode** + Duration」；「至少记录：……结果、**错误码**、耗时、PreviousHash、CurrentHash」（§3.4.4 `:1587`、§7.7 `:3380`） | 字段缺失 | 修订 |

### J 组　算法规则、错误码与接口（KeyType 兼容、GCM Counter、SM2/CTR/随机数长度、NodeId、废弃需求残留等）

| 序号 | 旧设计位置（文件+小节/行号） | 旧设计结论（原文摘要或引文） | V2.9 新要求（引文+需求编号/小节号） | 冲突性质 | 建议处置 |
| --- | --- | --- | --- | --- | --- |
| 58 | 详细设计文档.md §1.4 `:94-111`、§29 `:3131-3171`、§29.1 `:3175-3187`；核心模块 §3.2 `:414-427`；实现规范-CryptoService.md `:248-275`；实现规范-KeyService.md `:372-385` | 强制校验链「`调用方身份 → Key.OwnerAppId → KeyType → KeyUsage → Operation`，任一环节不匹配均拒绝执行」；`Key.KeyUsage 与操作匹配`；`ValidateKeyTypeAndUsage` 白名单含 `("SM2","SIGN")`、`("ROOT","WRAP")` 等 | 「**平台不校验 KeyUsage 业务语义**，由三方系统自行决定；**平台校验 KeyType 兼容性**（见 3.1.7）」；§3.1.7 仅定义 SM2/SM4/HMAC 三类 KeyType 的允许操作（§3.2.1 `:848`、§3.3.1 `:1193`、§6.5 `:2782`、§8.4 `:3801`；F-AUTH-021 已 DEPRECATED `:1515`） | 模型冲突 | 重写 |
| 59 | 详细设计文档.md §1.4 `:94-111`；实现规范-KeyService.md §34.5 `:422-426` | 无 KeyType 兼容性错误码；错误码为 `INVALID_KEY_TYPE_USAGE` 等字符串 | 「不兼容操作返回错误码 **12003 `KEY_TYPE_MISMATCH`**」（§3.1.7 `:832`；§6.11 权限/资源归属段 `:2952`） | 需新增 | 新增 |
| 60 | 详细设计文档.md §19.1 `:2419-2423`；§4.6 `:936-943`；核心模块 §3.5.4 `:523`；实现规范-CryptoService.md §35.1 `:11-13` | 「**如果由调用方提供 Nonce**，平台必须校验长度及策略；如果由平台生成，应使用密码学安全随机源」「Nonce: 推荐 12 bytes (96 bit); **请求提供则校验长度**; 未提供则 CSPRNG 生成」；`Sm4EncryptRequest(..., string? Nonce, string? Aad)` | GCM 表「**平台强制生成**」「生成安全随机 Nonce；在密钥生命周期内保证不重复」「**不得传入 Nonce**」；`Nonce = KeyVersion(4B) + NodeId(4B) + MonotonicCounter(4B)`，「编码规则必须固定为上述 12 字节拼接结果」（§3.1.3 `:719`、`:732-740`） | 流程变更 | 重写 |
| 61 | 详细设计文档.md 全文；实现规范-认证授权.md 全文 | `NodeId` 在旧设计中 **零出现**；无节点标识概念 | 「NodeId 分配：由平台统一分配，节点重启后保持不变；节点退役后 NodeId 不再复用；NodeId 注册、心跳、冲突检测机制由详细设计确定（见 I-15）」；「NodeId 冲突 → 停止 GCM 运算，触发安全事件，需人工介入」（§3.1.3 `:749`、§3.5.3 `:1695`、§3.5.4 `:1710`） | 需新增 | 新增 |
| 62 | 详细设计文档.md §19.1 `:2419-2423` | 仅要求「GCM 模式必须避免同一密钥下 Nonce 重复。平台不能仅依赖数据库随机字符串生成器」——**无计数器持久化、无溢出处置** | 「**Counter 持久化**：每个 (KeyId, KeyVersion, NodeId) 三元组维护持久化计数器」；「**禁止回绕到 0 并继续使用当前 KeyVersion**；停止该 KeyVersion 的新 GCM 加密请求，自动触发该 Data Key 的 KeyVersion 轮换；**Root Key / KEK 轮换不得由溢出事件自动触发**」（§3.1.3 `:744-746`） | 需新增 | 新增 |
| 63 | 详细设计文档.md §21 `:2476-2504` | 使用计量字段 `operation_count/processed_bytes/last_used_at` 存于 KeyVersion；阈值触发异步轮换（**本地/单节点语义**） | 「依据 SP 800-38D，每个 KeyVersion 的 GCM 加密调用总次数（**全节点合计**）不得超过 **2³²**；平台必须按 KeyVersion 聚合**全节点**调用计数（集中服务或数据库统一聚合），建议在 **2³¹** 设置告警阈值」；「达到 2³² 上限前必须完成该 KeyVersion 的强制轮换并记录审计」（§3.1.3 `:748`） | 需新增 | 新增 |
| 64 | 核心模块详细设计与实现规范.md §3.5.3 `:510-517` | CTR 仅三条：「Nonce 处理：请求提供 → 校验长度; 未提供 → CSPRNG 自动生成」「不需要 Padding」「同一密钥下 Nonce 不得重复」「加密和解密使用完全相同的 Nonce」 | 「**CTR 计数器规则（V2.9 补充）**：计数器长度 16 字节；初始值平台默认生成安全随机 16 字节；按**无符号大端序 +1**；**溢出禁止回绕**；平台按 `KeyId + Counter` 短窗口重复检测；按 KeyVersion 聚合计数，**建议 2³¹ 告警阈值**」（§3.1.3 `:721-730`；§3.1.3 契约表 `:718`） | 需新增 | 新增 |
| 65 | 核心模块详细设计与实现规范.md §3.3.1 `:435`；详细设计文档.md §20 `:2440-2472` | 「Plaintext 长度 ≤ **4096 bytes**（或按 Provider 能力）」；SM2 章节无长度限制与错误码 | 「SM2 单次加密数据最大长度 **512 字节**」「SM2 单次签名数据最大长度：不限制（受请求体限制）」「超限错误码 **20001 `PARAM_DATA_TOO_LARGE`**」；「详细设计阶段应结合所选密码设备能力冻结默认值」（§3.1.1 `:663-671`） | 流程变更 | 修订 |
| 66 | 详细设计文档.md §4.11 `:1010-1027`；实现规范-CryptoService.md `:239-244` | 随机数请求仅 `{length, encoding}`；`GenerateRandomAsync` 直接透传 `req.Length`，**无上限校验** | 「**随机数单次请求最大输出长度独立定义**：默认上限 **64KB**；最大可配置上限 **1MB**」「随机数最大长度**不得复用 NF-PERF-014**」「超过上限返回 20001」（§3.1.5 `:789-791`） | 流程变更 | 修订 |
| 67 | 详细设计文档.md §4.3 `:851-863`、§18.3 `:2363-2377`、§33 `:3396-3418`；实现规范-REST-API.md §40.1 `:10-29` | 三套并行错误表示：成功响应 `"code": 0`（int）、错误分类字符串前缀 `AUTH_xxx/KEY_xxx/...`、异常体 `"code": "KEY_NOT_FOUND"`（字符串） | 统一数值分段：`10000-10099 认证 / 11000-11099 Token / 12000-12099 权限 / 20000-20099 参数 / 30000-30099 密码服务 / 40000-40099 密钥管理 / 50000-50099 系统 / 60000-60099 限流 / 70000-70099 密码设备 / 80000-80099 安全事件 / 90000-90099 数据完整性`（§6.11 `:2946-2961`） | 接口变更 | 重写 |
| 68 | 实现规范-REST-API.md §40.4 `:175-289`、§40.7 `:384-408` | 业务 API 端点共 10 个（SM4 ×2、SM2 ×4、SM3 ×1、HMAC ×2、random ×1）；**无算法能力查询、无密码模块自检** | 新增 `GET /api/v1/crypto/algorithms`（算法能力）、`POST /api/v1/crypto/self-test`（密码模块自检）（§6.5 `:2773-2774`；§6.9 `:2885`；§3.1.6 F-KAT-003 `:806`） | 需新增 | 新增 |
| 69 | 实现规范-REST-API.md §40.3 `:84`；详细设计文档.md §4.5 `:900-904` | 密钥管理端点统一挂在 `/api/v1/admin/keys`；业务侧无密钥入口 | 双入口模型：业务 API 为 `/api/v1/keys`（POST/GET/activate/disable/enable/rotate/revoke/destroy/import/backup/restore/verify-recovery/versions，§6.6 `:2805-2820`）；管理端入口为 `/api/v1/admin/keys`（§6.13.3 `:3018-3025`） | 接口变更 | 修订 |
| 70 | 详细设计文档.md §4.1.1 `:728-729`；实现规范-REST-API.md §40.3 | 创建密钥仅限管理 API（Token）；无"业务 API 创建密钥"概念，无配置开关 | 「密钥创建**双入口权限模型**」：业务 API 创建需受 `F-CFG-007` 控制；「纳入 §6.10.1 敏感操作幂等清单，必须携带 `Idempotency-Key`」「创建后状态为 CREATED，需管理员激活」「创建动作记录审计并通知安全管理员」「不允许指定 AppId」（§6.6 `:2822-2836`；`AllowApiKeyCreate` 字段 `:3200`） | 需新增 | 新增 |
| 71 | 详细设计文档.md §4.12 `:1029-1045`、§18.1 `:2320-2331`、§32.1 `:3340-3350`；实现规范-REST-API.md §40.3 | 幂等清单 **5 项**：rotate、revoke、destroy、token revoke、application secret reset | 敏感操作幂等清单 **18 项**：密钥轮换/销毁/注销/恢复/**导入**/**业务 API 创建密钥**、AppSecret 重置/轮换/撤销、Token 撤销、Root Key 轮换/销毁、KEK-Runtime 轮换/销毁、KEK-Backup 轮换/销毁、用户 MFA 重置/解绑、用户手机号变更、MFA 策略切换（§6.10.1 `:2892-2914`） | 流程变更 | 修订 |
| 72 | 详细设计文档.md §26.1 `:2804-2841`；核心模块 §2.1 `:134-150`；实现规范-KeyService.md §34.2 `:62-74` | `IKeyService` 均**不含** Backup / Restore / VerifyRestore（核心模块 `:155` 明确"移除 Backup/Restore/VerifyRestore（归入独立备份模块）"）；详细设计 §26.1 亦无（§13.x/§2.2.9-2.2.11 仅在设计层描述） | 密钥管理接口须含 `/backup`、`/restore`、`/verify-recovery`（§6.6 `:2817-2819`）；恢复验证要求见 §7.14 `:3596-3606`；Provider 能力检查见 §6.6 `:2848-2850` | 接口变更 | 新增 |
| 73 | 核心模块详细设计与实现规范.md §3.2 `:422`；详细设计文档.md §29 `:3148` | 授权校验链中含「11. 安全策略允许（**配额、限流**）」，将配额作为授权必要环节 | F-AUTH-022「配额管理」**DEPRECATED-V2.5**：「DoS 通过防火墙控制，**不在应用层实现业务配额**」（§3.3.6 `:1516`）；§8.4 业务 API 权限检查顺序中无配额环节（`:3761-3777`） | 已废弃 | 删除 |
| 74 | 详细设计文档.md §5.4 `:1110-1114`；实现规范-认证授权.md §38.2 `:201-208` | JWT 以 `scope: ["key:manage","app:manage","audit:read"]` 表达授权，`GetScopesForRole` 按角色返回 scope 数组 | 改为 `PERM_*` 权限矩阵 + RBAC + **三员互斥**（§3.6.1 `:1746-1770`、§8.4 `:3779-3795`）。F-AUTH-020「API 权限分配」**DEPRECATED-V2.5**：「应用注册并通过 AppId 认证后即可调用」（§3.3.6 `:1514`） | 已废弃 | 修订 |
| 75 | 详细设计文档.md §2.4 `:433-463`；§15 无设备表；实现规范-数据库DDL.md §32.6 `:122-145`；实现规范-EFCore实体与配置.md `:177-194` | `sys_crypto_device` 字段：device_id/device_name/vendor/model/serial_number/endpoint/provider_type/status/priority/is_primary/last_health_at/error_count | CryptoDevice 须含 `DeviceType`、`SupportedAlgorithms`、**`ProviderCapabilities`（V2.9 新增，JSON）**、`MasterSlaveRelation`、`CertNo`、`CertAuthority`、`CertExpireAt`、`IntegrityValue`（§7.9 `:3420-3436`；F-DEV-001～010 `:1866-1877`） | 字段缺失 | 修订 |
| 76 | 详细设计文档.md §1.1 `:20-35`；§1.6.2 `:166-172`；§5.3 `:1086-1087`；实现规范-管理员密码认证与密码存储.md §2.1 `:35-44`、§2.2 `:46-54` | 管理员密码哈希默认 **PBKDF2-HMAC-SHA256**，迭代 600,000（备选 PBKDF2-SM3，迭代 100,000）；而详细设计 §1.6.3 `:181` 的 KDF 为 **PBKDF2-HMAC-SM3 ≥600,000**（旧设计内部参数不一致） | 「Hash 算法：**PBKDF2-SM3**（参数在详细设计前冻结）」「Salt：每用户独立随机」「迭代次数：按 OWASP 建议（详细设计确定）」（§3.6.2 `:1800-1802`）；F-RK-009「使用 **PBKDF2-SM3** 派生 KEK-Unlock；Salt、迭代次数、输出长度等参数必须在详细设计阶段冻结」（`:2120`） | 模型冲突 | 修订 |
| 77 | 实现规范-管理员密码认证与密码存储.md §4.1 `:96-106`；实现规范-数据库DDL.md §32.7 `:150-163` | `sys_user` 密码相关字段：`password_hash`、`password_algorithm`、`password_version`、`password_changed_at`、`must_modify_pwd` | User 实体须为 `PasswordHash`（PBKDF2-SM3）、`PasswordSalt`（每用户独立随机）、`PasswordUpdatedAt`（§7.4 `:3236-3238`、§7.17.1 `:3629`）；密码复杂度/最小长度 ≥12/历史 5 次/有效期 90 天（§3.6.2 `:1790-1802`） | 字段缺失 | 修订 |
| 78 | 详细设计文档.md §4.1.1 `:730-731`；§5.8 `:1167-1180`；实现规范-数据库DDL.md §32.11 `:260-273` | `sys_config` 字段：config_key/config_value/description/updated_by/updated_at；**无 ConfigScope**；配置项清单中无 `MFA_POLICY_MODE`/`F-CFG-006`/`F-CFG-007` | `SystemConfig` 须含 `ConfigScope` + `IntegrityValue`（§7.10 `:3440-3449`、§7.17.1 `:3637`）；须支持 `MFA_POLICY_MODE`（F-CFG-006）、业务 API 创建密钥开关 `F-CFG-007`（§3.3.4 `:1421`、§6.6 `:2831`） | 字段缺失 | 修订 |
| 79 | 实现规范-审计与安全事件.md §39.4 `:254-261`；详细设计文档.md §22 `:2508-2540`；实现规范-数据库DDL.md §32.10 `:232-258` | `DetermineSeverity` 映射 6 类（AUTH_FAILURE/IP_POLICY_VIOLATION→MEDIUM；APP_SECRET_BRUTE_FORCE/TOKEN_REPLAY→HIGH；HSM_OFFLINE/AUDIT_INTEGRITY_FAILURE→CRITICAL；KEY_ROTATION_REQUIRED→LOW）；事件字段含 `handling_result` | CRITICAL 判定须含「Root Key 风险、审计完整性断裂、**外部锚点失败**、HSM 全面不可用、**数据完整性大面积失败**、**密码模块自检失败**、**NodeId 冲突**」；HIGH 须含 KeyId 越权、大规模解密/签名、认证证书过期（§3.5.4 `:1706-1714`、§3.5.3 `:1688-1695`）；SecurityEvent 字段口径须含 `HandleResult`（§7.17.1 `:3635`） | 流程变更 | 修订 |

### 汇总

- **过时/冲突条目总数：79 条**（A 组 6 + B 组 6 + C 组 5 + D 组 10 + E 组 8 + F 组 11 + G 组 3 + H 组 4 + I 组 4 + J 组 22）。
- **按冲突性质分布**（每条按主性质唯一归类，合计恰为 79）：

| 冲突性质 | 条数 | 序号 |
| --- | ---: | --- |
| 模型冲突 | 12 | 8、9、15、16、18、19、22、23、36、44、58、76 |
| 字段缺失 | 11 | 7、13、24、25、39、47、53、57、75、77、78 |
| 流程变更 | 14 | 2、3、10、12、41、42、46、50、51、60、65、66、71、79 |
| 接口变更 | 13 | 1、5、28、29、30、32、33、34、40、43、67、69、72 |
| 已废弃 | 5 | 20、21、49、73、74 |
| 需新增 | 24 | 4、6、11、14、17、26、27、31、35、37、38、45、48、52、54、55、56、59、61、62、63、64、68、70 |
| **合计** | **79** | — |

- **按组分布**：A 6 / B 6 / C 5 / D 10 / E 8 / F 11 / G 3 / H 4 / I 4 / J 22。
- **产出文档影响面**：12 份旧设计文档**全部**需要修订，无一份可原样保留；其中 `详细设计文档.md`、`核心模块详细设计与实现规范.md`、`实现规范-数据库DDL.md`、`实现规范-EFCore实体与配置.md`、`实现规范-认证授权.md`、`实现规范-数据库DDL.md` 受影响最重（表结构 + 认证链路全量重写）。

### 最严重冲突（供变更说明优先级排序）

| 级别 | 序号 | 冲突概要 | 不可回避的原因 |
| --- | --- | --- | --- |
| P0 | 1、6 | 业务 API 签名串构造公式与 Canonical Request 规范化规则完全不同 | 客户端与服务端必须同时按同一公式实现，公式不符将导致**全部业务 API 认证失败**，且跨语言实现无测试向量可依 |
| P0 | 7、8、9 | AppSecret 存储模型（仅 secret_hash → SecretCiphertext + SecretHash）与验签路径 | 旧模型在密码学上**不可实现**（哈希不可逆，无法取回 AppSecret 参与 HMAC）；旧设计代码注释（实现规范-认证授权.md `:287-289`）已自认该缺陷，属根本性阻塞项 |
| P0 | 13、14 | 全部关键表缺失 IntegrityValue，且无 IntegrityKey | 直接对应密评条款 NF-CMP-016（防御具有数据库写权限的篡改者），属"要么实现、要么无法通过密评"的合规硬要求 |
| P0 | 18、19 | 密钥分层模型（旧设计 3～5 层混合表述）→ L0/L1/L2/L3 四层 | 影响 Root Key 存储、加载验证、轮换、备份恢复、AppSecret 保护、手机号保护**全部下游设计**，返工面最大 |
| P1 | 28、29 | Provider 强制统一接口 → Capability 最小准入集 + 可选扩展接口 | 直接决定 HSM 厂商适配可行性；若按旧强制接口实现，多数国产 HSM 难以通过准入（CanWrapKey/CanUnwrapKey 强制项），将阻塞设备选型与上线 |
| P1 | 36、37、38 | 管理端认证缺 AdminSession / RefreshToken / MFA 全局强制 | 等保三级"双因素身份鉴别"与"空闲超时"两项要求均无法满足，属上架前必须闭环项 |

---

## 第三部分　可继承资产

> 判定口径：「可直接沿用」= 与 V2.9 无冲突，可逐字/逐结构迁移；「少量修订」= 骨架与结论成立，仅需按第二部分清单补字段、改口径、换参数。

| 序号 | 资产 | 文件与章节/行号 | 处置 | 说明 |
| --- | --- | --- | --- | --- |
| 1 | **14 张核心表清单与设计约束**（雪花 ID、无数据库外键、owner_app_id 存 app_id、多对多角色、updated_at 拦截器、索引总长度待验证） | 核心模块详细设计与实现规范.md §1.3 `:79-109` | 少量修订 | 约束全部成立；表清单需扩至 22+（补 KeyMaterial/AdminSession/RefreshToken/SignatureNonce/UserMfaBinding/UserDataDEK/RootKeyMetadata/KekMetadata/BackupRecord/AlertRule/IntegrityScanRecord） |
| 2 | **雪花 ID 生成策略与会话主键实现**（64 位 41+10+12、纪元 2020-01-01、`ValueGeneratedNever()` + SnowflakeIdInterceptor） | 实现规范-EFCore实体与配置.md §33.5 `:530-563` | 可直接沿用 | 与 V2.9 无冲突，仅需扩展实体注册 |
| 3 | **sys_key / sys_key_version / sys_key_authorization 建表语句骨架**（含索引命名 uk_/ix_ 规范） | 实现规范-数据库DDL.md §32.3-§32.5 `:41-120`；详细设计文档.md §16 `:2172-2238` | 少量修订 | 结构、索引、命名规范可留；须删 key_usage、改 key_type 取值域、补 ProviderId/StorageType/ProviderKeyIdentity/MaterialVersion/IntegrityValue |
| 4 | **EF Core Entity + Fluent API 配置风格**（`IEntityTypeConfiguration`、`ValueGeneratedNever`、`IsConcurrencyToken`、复合唯一索引） | 实现规范-EFCore实体与配置.md §33.2-§33.3 `:85-495` | 少量修订 | 配置模板可逐份复用；仅需按新实体扩展 |
| 5 | **Saga 补偿式密钥创建事务**（DB 提交 Key → Provider 生成 → DB 写 Version；补偿任务表 + 最多 3 次指数退避 + 孤儿密钥销毁 + CRITICAL 事件；"绝不将孤儿 ProviderKeyRef 返回业务系统"） | 核心模块详细设计与实现规范.md §2.2.1 `:161-201`；详细设计文档.md §13.2 `:1722-1787` | 可直接沿用 | 与 V2.9 无冲突；仅需在 Provider 调用处插入能力检查 |
| 6 | **并发控制与串行化设计**（分布式锁 `lock:key-rotate:{KeyId}` + `SELECT ... FOR UPDATE` 双重保护 + "轮换必须重新读取 current_version，不能使用客户端提交的旧 VersionNo"） | 核心模块详细设计与实现规范.md §2.2.4 `:232-259`；详细设计文档.md §13.3-§13.4 `:1789-1841` | 可直接沿用 | 与 V2.9 无冲突；建议修正实现规范-KeyService.md `:355-361` 中以 `Entry.State = Modified` 冒充行锁的写法 |
| 7 | **两阶段销毁流程骨架**（申请 → `DestroyRequestId` + 有效期 → 确认 → Provider 销毁 → 结果验证 → 清材料 → 审计） | 详细设计文档.md §13.5 `:1843-1895`；核心模块详细设计与实现规范.md §2.2.7 `:286-311` | 少量修订 | 骨架可直接沿用；须叠加 §3.6.4 高风险二次确认（MFA/口令）与 ProviderCapabilities 检查；须统一"单阶段 vs 两阶段"的旧设计内部矛盾 |
| 8 | **Provider 防腐层与厂商 SDK 隔离**（Abstractions 零外部依赖；Hsm.VendorX 独立项目；业务层只引用 Abstractions；新增厂商 8 步不修改业务层） | 核心模块详细设计与实现规范.md §4.1 `:543-579`、§4.5 `:657-679`；详细设计文档.md §14.1 `:1928-1954` | 可直接沿用 | 架构原则完全成立，是 Provider 能力模型改造的现成承载点 |
| 9 | **HSM 连接池实现**（Channel + SemaphoreSlim + 健康会话回收 + 连接复用/并发上限） | 实现规范-HSM适配器.md §37.4 `:118-196` | 可直接沿用 | 与 V2.9 无冲突 |
| 10 | **Provider 错误码映射表与兜底策略**（厂商码内部保留、外部只返回统一平台错误码） | 实现规范-HSM适配器.md §37.5 `:198-224`；核心模块详细设计与实现规范.md §4.6 `:681-696`；详细设计文档.md §2.7 `:492-507` | 少量修订 | 需补 `NotSupported` 与 `PROVIDER_CAPABILITY_NOT_SUPPORTED` 的落点 |
| 11 | **超时分层与重试策略**（API 30s > Service 15s > Provider 5-10s；Hash/Verify 可重试；GenerateKey/DestroyKey/Sign 禁止自动重试；超时后经 GetKeyInfo 确认最终状态，否则进 UNKNOWN） | 核心模块详细设计与实现规范.md §4.7 `:698-714`；详细设计文档.md §14.5 `:2061-2078`、§32.3 `:3372-3394` | 可直接沿用 | 与 V2.9 §2.6 无冲突 |
| 12 | **健康检查规范**（路由前查缓存 + 30s 定时全量 + 失败即时；3 次失败 → DEGRADED，5 次 → OFFLINE） | 核心模块详细设计与实现规范.md §4.8 `:716-734` | 可直接沿用 | 与 V2.9 §3.5.3 HSM 健康检查阈值（3 次连续/3 分钟）口径一致 |
| 13 | **Provider 路由优先级四条**（已绑定 Provider 优先 → 新 Key 用默认 → 默认不可用按 HA 策略 → 不允许未配置自动把 HSM 密钥转软件） | 核心模块详细设计与实现规范.md §4.3 `:640-645`；详细设计文档.md §2.3 `:415-431` | 少量修订 | 第 4 条须升级为"部署模式互斥"；解析维度须补 ProviderId / StorageMode / ProviderCapabilities |
| 14 | **ICryptoService 分层与全部算法 DTO**（SM2/SM3（含流式）/SM4/HMAC/Random 共 11 方法 + record DTO + 解码/编码约定） | 实现规范-CryptoService.md §35.1-§35.2 `:5-80`；核心模块详细设计与实现规范.md §3.1 `:359-397` | 少量修订 | 接口与 DTO 结构可留；须补 appId、长度上限、平台生成 Nonce、完整性值校验 |
| 15 | **SM3 / HMAC-SM3 / SM4-CBC 运算实现约定**（SM3 HEX 默认 + 空数据摘要 + 流式；HMAC 常量时间比较 `FixedTimeEquals`；CBC IV 16B + PKCS#7 + 同密钥不重复 IV） | 核心模块详细设计与实现规范.md §3.4-§3.5 `:463-508`；实现规范-CryptoService.md `:205-235` | 可直接沿用 | 与 V2.9 §3.1.2/§3.1.3 一致 |
| 16 | **GCM Tag 校验失败四步处置**（不返回部分明文 → 记录失败审计 → 统一错误 → 不暴露校验位置） | 核心模块详细设计与实现规范.md §3.5.4 `:529-534`；详细设计文档.md §19.3 `:2429-2436` | 可直接沿用 | V2.9 §3.1.3 `:710` 要求一致 |
| 17 | **Controller 分层、路由风格与统一响应包装**（`[ApiController]` + `[Route]` + `ApiResponse<T>` + RequestId 透传） | 实现规范-REST-API.md §40.1 `:5-38`、§40.3-§40.4 `:78-289` | 少量修订 | 骨架可留；`Code` 须改数值分段、路径须按 §6.5/§6.6/§6.13 调整、须补 algorithms/self-test |
| 18 | **全局异常处理中间件**（BusinessException→400 / CryptoProviderException→503 / 未处理→500；统一不外泄堆栈与密钥内容） | 实现规范-REST-API.md §40.5 `:291-344` | 可直接沿用 | 与 V2.9 §6.4 `:2755` 要求一致 |
| 19 | **错误码分层框架与异常类型**（AUTH_/PERMISSION_/KEY_/CRYPTO_/PROVIDER_/DEVICE_/VALIDATION_/SYSTEM_/AUDIT_ 九层 + BusinessException/CryptoProviderException） | 详细设计文档.md §18.3 `:2363-2377`、§33 `:3396-3418`；实现规范-KeyService.md §34.5 `:411-427` | 少量修订 | 分层思想可留；须映射到 V2.9 §6.11 的 10000-90099 数值分段 |
| 20 | **幂等机制与幂等记录表**（`Idempotency-Key` + `IdempotencyScope = AppId+Method+Path+Key` + request_hash 相同回放/不同报 `IDEMPOTENCY_KEY_REUSED` + sys_idempotency_record DDL） | 详细设计文档.md §18.1-§18.2 `:2320-2361`、§32.1-§32.2 `:3340-3370`；实现规范-数据库DDL.md §32.12 `:275-294` | 少量修订 | 机制与表结构可直接沿用；清单须由 5 项扩至 18 项（§6.10.1） |
| 21 | **审计服务与哈希链服务骨架**（IAuditService 双重载 + AuditLogEntry + 从 HttpContext 取 Operator/SourceIp + VerifyChain 区间校验） | 实现规范-审计与安全事件.md §39.1-§39.3 `:5-179` | 少量修订 | 服务骨架可留；ComputeHash 须整体替换为 CanonicalSerialize + SM3，去 Redis 单点，补分片与锚点 |
| 22 | **安全事件服务与后台 Worker 骨架**（ISecurityEventService + RaiseAsync 自动判级 + GetOpenEvents + CloseAsync + SecurityDetectionWorker/KeyExpirationWorker + DI） | 实现规范-审计与安全事件.md §39.4-§39.6 `:181-370`；详细设计文档.md §22 `:2508-2562` | 少量修订 | 骨架可留；判级映射须补 CRITICAL 类型，须新增完整性巡检 Worker 与锚点 Worker |
| 23 | **IPasswordHasher 抽象与密码透明升级策略**（接口 + Factory + 自描述哈希格式 `{algorithm}${version}${salt}${iterations}${hash}` + NeedsUpgrade + 登录后自动升级 + 不回退） | 实现规范-管理员密码认证与密码存储.md §3 `:67-93`、§5 `:120-153`、§7.1-§7.2 `:185-206` | 少量修订 | 抽象与格式设计优秀可留；默认算法须由 PBKDF2-SHA256 改为 PBKDF2-SM3，迭代参数须在详细设计阶段一次性冻结 |
| 24 | **登录失败控制与安全事件上报**（5 次失败锁 30 分钟 / 成功清零 / `ADMIN_LOGIN_FAILED`、`ADMIN_ACCOUNT_LOCKED`、`ADMIN_LOGIN_SUCCESS` 三类事件 / must_modify_pwd 首次强制改密） | 实现规范-管理员密码认证与密码存储.md §6 `:157-180` | 可直接沿用 | 与 V2.9 §3.5.3 `:1680`、§3.6.2 无冲突 |
| 25 | **IKeyAuthorizationChecker 密钥授权模型**（Owner 直通 + 显式 sys_key_authorization + permissions 含操作 + 状态与有效期校验） | 实现规范-认证授权.md §38.4 `:339-378`；核心模块详细设计与实现规范.md §6.4 `:885-901`；详细设计文档.md §3.3 `:629-651` | 少量修订 | 模型与接口可留；permissions 枚举须去除 KeyUsage 语义，叠加 KeyType 兼容性与资源归属校验 |
| 26 | **Key 状态机与状态-操作矩阵骨架**（状态定义表 + 转换表 + 矩阵 + `*` 附条件说明） | 详细设计文档.md §1.7 `:242-286`、§29.1 `:3175-3187` | 少量修订 | 骨架可留；须补 KeyVersion 独立状态机（§3.2.3），修正 REVOKED/EXPIRED 历史解密验签口径（§3.2.22），补 HMAC Generate 归入签名类 |
| 27 | **国产数据库适配矩阵**（MySQL 8.4 / 达梦 DM8 / 人大金仓 KingbaseES 的主键、datetime 精度、大字段、JSON、索引长度、布尔、ON UPDATE、字符集对照；DDL 末尾 8 项适配要点） | 核心模块详细设计与实现规范.md §1.4 `:111-124`；实现规范-数据库DDL.md §32.13 `:296-308` | 可直接沿用 | 适配结论与实测项清单可直接进新文档 |
| 28 | **技术栈与依赖版本基线**（.NET 10 / C# 14 / EF Core 10 / MySql.EntityFrameworkCore 10.0.x（Pomelo 不支持 EF Core 10）/ BouncyCastle 2.5+ / Serilog / Scalar / FluentValidation 12 + 国产中间件替代说明） | 核心模块详细设计与实现规范.md §5.1-§5.5 `:738-791` | 少量修订 | 依赖清单可留；JWT 行须改为 SM2 签名 Token，须补安全内存缓冲区与 SM3/HMAC 实现依赖 |
| 29 | **项目结构与依赖方向约束**（13 个项目分层；允许/禁止依赖清单，含 `Api → Crypto.Hsm.*`、`Application → 厂商 SDK`、`Domain → EF Core` 禁止项） | 核心模块详细设计与实现规范.md §5.6-§5.7 `:793-834`；详细设计文档.md §34 `:3420-3476` | 可直接沿用 | 与 V2.9 §2.2 分层要求一致；须统一 §37.1 与 §5.6 的项目布局差异 |
| 30 | **第一阶段编码顺序与禁止事项**（Domain → Persistence → Provider Abstraction → HSM Adapter → Software Provider → KeyService → CryptoService → Auth → Authz → Audit → API → HA；8 条禁止事项） | 详细设计文档.md §23 `:2566-2631`、§35 `:3478-3498`；核心模块详细设计与实现规范.md §4.5 `:657-679` | 少量修订 | 顺序可作新版实施顺序底稿；须插入完整性值、Provider 能力准入、AdminSession/RefreshToken/MFA、短缓存、锚点等任务 |
| 31 | **测试与验收矩阵骨架**（密钥管理 15 项 / Provider 9 项 / 安全 11 项 / 密码算法 / 性能验收条件 + 需求追踪矩阵结构 `需求编号 → 设计章节 → API → 数据表 → 测试用例 → 验收结果`） | 详细设计文档.md §9 `:1426-1556` | 少量修订 | 结构与验收条件口径可留；须补 V2.9 第 10 章新增用例（数据完整性、请求签名专项、Root Key/KEK、GCM Nonce 与 Counter、审计哈希链、密钥体系恢复） |
| 32 | **HA / 灾备设计**（应用层无状态 + Session 不落本地；Redis 用途与"密码核心操作不得依赖 Redis"原则；DB 主备；HSM 双设备主备能力一致、不允许故障切换时降低安全等级；RTO ≤30min / RPO ≤5min） | 详细设计文档.md §7 `:1290-1371` | 可直接沿用 | 与 V2.9 §4.2/§4.3 一致；仅需补"缓存不可用按 NF-AVAIL-006 降级"引用 |
| 33 | **数据库最小权限与时间同步要求**（runtime / migration / audit read-only / backup 四类账号；禁止 root/sa；全节点统一 NTP 及其影响面） | 详细设计文档.md §8.3-§8.4 `:1399-1422` | 可直接沿用 | 与 V2.9 §7.15 `:3608-3610`、§4.11 一致 |
| 34 | **设计约束与待确认项清单**（HSM 厂商/型号/SDK、国产数据库产品与 Provider、SM2 签名与密文格式、SM4-GCM 支持、HMAC-SM3 支持、密钥备份同步、国产 CPU/OS、生产是否强制 HSM、流式接口能力、国密 TLS 接入方式） | 详细设计文档.md §10 `:1558-1623`、§24 `:2635-2672`、§36.2 `:3519-3534` | 可直接沿用 | 清单本身与 V2.9 第 5 章、第 10 章 I-* 待确认项高度互补，可直接并入新版"待确认项"章节 |
| 35 | **敏感字段标识与全局禁止存储清单**（secret_hash / provider_key_ref / encrypted_key_material / password_hash / audit hash 字段的安全要求；全局禁止存储 6 项） | 核心模块详细设计与实现规范.md §1.2 `:66-77`；详细设计文档.md §3.1 `:513-532`、§3.4 `:678-685`；§8.9 对应 | 少量修订 | 表格结构可留；`secret_hash` 行须改为 SecretCiphertext + SecretHash，须新增 IntegrityKey 行，须补明文手机号与短信验证码禁止项（V2.9 §8.9 `:3835`） |
| 36 | **密钥包装层级图与"业务 API 只接收 keyId 不接收密钥材料"铁律** | 详细设计文档.md §30.2 `:3222-3247`、§31 `:3249-3288`、§11 `:1659-1666`；§23.1 `:2624-2631` | 可直接沿用 | 原则与禁止清单完全符合 V2.9 §1.7.1；仅层级图内容须替换为四层模型 |
| 37 | **统一响应体结构与 RequestId/TraceId 全链路透传**（`{code,message,data,requestId,timestamp}`；`API → Auth → Authorization → KeyService → CryptoProvider → Audit` 透传） | 详细设计文档.md §4.3 `:851-863`、§6.3 `:1220-1228`；实现规范-REST-API.md §40.1 `:5-38` | 可直接沿用 | 与 V2.9 §6.1 `:2580`、§7.7 `:3380` 一致 |
| 38 | **管理端 API 与管理端接口路径前缀约定**（`/api/v1/admin/**` 与业务 API 隔离部署） | 核心模块详细设计与实现规范.md §5.6 `:797`、§6.1 `:844-846`；实现规范-REST-API.md §40.2-§40.3 `:45-84` | 可直接沿用 | 与 V2.9 §6.1 `:2582-2587`、§6.13 `:2972` 一致 |

> **可继承资产统计**：共 38 项。其中【可直接沿用】16 项、【少量修订】22 项。**无一项旧设计资产需要整体废弃**——旧设计的工程骨架（分层、防腐层、事务补偿、并发控制、连接池、雪花 ID、适配矩阵、测试矩阵）质量较高，主要缺陷集中在**数据模型字段、认证/密钥/审计的密码学结论、以及 Provider 能力契约**三个维度。

---

## 附：本文档使用方法（给架构师）

1. **写"设计变更说明"章节**时：直接引用第二部分的 79 条，按"P0 → P1 → P2"顺序排列；每条已给出"旧结论 → 新要求 → 冲突性质 → 处置"，可直接改写为变更条目。
2. **变更说明的章节骨架建议**：
   - 3.1 认证模型变更（A/B/H 组，21 条）
   - 3.2 密钥体系变更（D 组，10 条）
   - 3.3 Provider 能力模型变更（E 组，8 条）
   - 3.4 管理端认证与 MFA 变更（F 组，11 条）
   - 3.5 审计与完整性变更（C/I 组，9 条）
   - 3.6 数据模型变更（G 组 + J 组字段类，约 12 条）
   - 3.7 算法规则与接口契约变更（J 组算法/接口类，约 10 条）
   - 3.8 已废弃机制说明（20、21、49、73、74，5 条）
   - 3.9 未实现需新增能力清单（24 条）
3. **新增设计时的"验收自检项"**：凡涉及 secret_hash、key_usage、ROOT KeyType、Nonce 由调用方传入、Timestamp ±5 分钟、Token 2 小时、appSecret 24 小时兼容期等表述的段落，均为旧设计残留，须逐一确认已替换。
4. **严禁直接沿用**的旧结论清单（若在新文档中出现即为缺陷）：
   - 使用 `secret_hash` 做 HMAC-SM3 验签（含"PBKDF2 还原"表述）
   - `key_usage` 字段及 KeyUsage 业务语义校验
   - `key_type = 'ROOT'` 或 `key_usage = 'WRAP'`
   - "AppSecret 轮换期间旧版本保持 24 小时有效"
   - "开发/测试环境允许配置文件明文存储 Root Key"
   - "软件 Provider 可在 HSM 故障时作为降级执行原 HSM 密钥操作"
   - "Token 有效期 2 小时"
   - "Nonce 由调用方提供并仅存 Redis、TTL 10 分钟"
   - "MFA 为可选能力 / 后续扩展"

---

*文档结束。本盘点为只读操作，未修改任何旧设计文件。*