using CryptoPlatform.Domain.Enums;

namespace CryptoPlatform.Domain;

/// <summary>
/// 密钥生命周期状态转换验证器。
/// 基于需求文档 3.2.4 定义的状态转换矩阵，白名单模式。
/// </summary>
public static class KeyStatusTransition
{
    /// <summary>
    /// 允许的状态转换集合（14 条）
    /// </summary>
    private static readonly Dictionary<(KeyStatus From, KeyStatus To), string> AllowedTransitions = new()
    {
        { (KeyStatus.CREATED,  KeyStatus.ACTIVE),   "激活权限" },
        { (KeyStatus.CREATED,  KeyStatus.REVOKED),  "创建后发现异常" },
        { (KeyStatus.CREATED,  KeyStatus.DESTROYED),"创建失败清理" },
        { (KeyStatus.ACTIVE,   KeyStatus.ROTATED),  "新版本产生" },
        { (KeyStatus.ACTIVE,   KeyStatus.DISABLED), "临时禁用" },
        { (KeyStatus.ACTIVE,   KeyStatus.EXPIRED),  "系统自动到期" },
        { (KeyStatus.ACTIVE,   KeyStatus.REVOKED),  "安全事件/撤销权限" },
        { (KeyStatus.ACTIVE,   KeyStatus.DESTROYED),"满足销毁条件" },
        { (KeyStatus.DISABLED, KeyStatus.ACTIVE),   "重新激活" },
        { (KeyStatus.DISABLED, KeyStatus.REVOKED),  "永久撤销" },
        { (KeyStatus.DISABLED, KeyStatus.DESTROYED),"销毁" },
        { (KeyStatus.ROTATED,  KeyStatus.DESTROYED),"满足保留期" },
        { (KeyStatus.EXPIRED,  KeyStatus.DESTROYED),"销毁" },
        { (KeyStatus.REVOKED,  KeyStatus.DESTROYED),"满足销毁条件" },
    };

    /// <summary>
    /// 判断状态转换是否允许
    /// </summary>
    public static bool IsAllowed(KeyStatus from, KeyStatus to)
        => AllowedTransitions.ContainsKey((from, to));

    /// <summary>
    /// 验证状态转换，不允许时抛出异常
    /// </summary>
    /// <exception cref="InvalidOperationException">状态转换不被允许</exception>
    public static void Validate(KeyStatus from, KeyStatus to)
    {
        if (!IsAllowed(from, to))
        {
            throw new InvalidOperationException(
                $"不允许的密钥状态转换: {from} → {to}");
        }
    }

    /// <summary>
    /// 获取状态转换的条件描述（用于审计日志）
    /// </summary>
    public static string GetCondition(KeyStatus from, KeyStatus to)
        => AllowedTransitions.TryGetValue((from, to), out var condition)
            ? condition
            : "N/A";

    /// <summary>
    /// 获取指定状态允许转换到的目标状态集合
    /// </summary>
    public static IReadOnlyList<KeyStatus> GetAllowedTargets(KeyStatus from)
        => AllowedTransitions.Keys
            .Where(k => k.From == from)
            .Select(k => k.To)
            .ToList();
}
