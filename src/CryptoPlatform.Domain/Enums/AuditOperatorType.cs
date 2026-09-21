namespace CryptoPlatform.Domain.Enums;

/// <summary>
/// 审计日志操作者类型
/// </summary>
public enum AuditOperatorType
{
    /// <summary>接入应用</summary>
    APP,

    /// <summary>管理用户</summary>
    ADMIN,

    /// <summary>普通用户</summary>
    USER,

    /// <summary>系统自动</summary>
    SYSTEM
}
