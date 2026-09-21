namespace CryptoPlatform.Application;

/// <summary>
/// 操作上下文。提供当前请求的调用方信息和请求标识。
/// Web 场景从 HTTP Header 读取；后台任务/测试场景返回默认值。
/// </summary>
public interface IOperationContext
{
    /// <summary>当前调用方应用 ID（来自 X-App-Id 或 Admin Token）</summary>
    string AppId { get; }

    /// <summary>当前请求 ID（用于审计追踪）</summary>
    string RequestId { get; }

    /// <summary>操作者类型：APP / ADMIN / SYSTEM</summary>
    string OperatorType { get; }

    /// <summary>操作者 ID</summary>
    string? OperatorId { get; }
}
