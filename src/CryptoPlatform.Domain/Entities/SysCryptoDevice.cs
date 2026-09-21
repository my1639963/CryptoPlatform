namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 密码设备注册
/// </summary>
public sealed class SysCryptoDevice
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = null!;
    public string DeviceName { get; set; } = null!;
    public string Vendor { get; set; } = null!;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Endpoint { get; set; }
    public string ProviderType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int Priority { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime? LastHealthAt { get; set; }
    public long ErrorCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
