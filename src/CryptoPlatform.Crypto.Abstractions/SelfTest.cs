namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>自检触发方式（需求 V2.10 §3.1.6 F-KAT-001～003）。</summary>
public enum SelfTestTrigger
{
    /// <summary>上电自检</summary>
    PowerOn,

    /// <summary>周期自检（默认 24 小时）</summary>
    Periodic,

    /// <summary>管理员手动触发</summary>
    OnDemand,
}

/// <summary>自检请求。</summary>
public sealed record SelfTestRequest
{
    /// <summary>触发方式</summary>
    public SelfTestTrigger Trigger { get; init; } = SelfTestTrigger.OnDemand;

    /// <summary>是否包含 SM2 签名 KAT（需测试专用密钥与测试专用路径）</summary>
    public bool IncludeSm2KAT { get; init; } = true;
}

/// <summary>单项自检结果。</summary>
public sealed record SelfTestItem(string Algorithm, bool Passed, string? Detail);

/// <summary>
/// 密码模块自检结果（需求 V2.10 §3.1.6：覆盖 SM2/SM3/SM4/HMAC-SM3/随机数；
/// 使用 GM/T 标准测试向量；失败时密码模块标记不可用并拒绝所有密码运算）。
/// </summary>
public sealed class SelfTestResult
{
    /// <summary>整体结论</summary>
    public string OverallStatus { get; init; } = "UNKNOWN";   // PASSED / FAILED / NOT_SUPPORTED

    /// <summary>是否全部通过</summary>
    public bool Passed => string.Equals(OverallStatus, "PASSED", StringComparison.Ordinal);

    /// <summary>分项结果</summary>
    public IReadOnlyList<SelfTestItem> Items { get; init; } = [];

    /// <summary>执行时间</summary>
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.UtcNow;
}
