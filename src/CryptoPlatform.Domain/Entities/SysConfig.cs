namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 系统配置
/// </summary>
public sealed class SysConfig
{
    public long Id { get; set; }
    public string ConfigKey { get; set; } = null!;
    public string ConfigValue { get; set; } = null!;
    public string? Description { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}
