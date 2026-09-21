namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 接入应用
/// </summary>
public sealed class SysApplication
{
    public long Id { get; set; }
    public string AppId { get; set; } = null!;
    public string AppName { get; set; } = null!;
    public byte Status { get; set; }
    public string? IpWhitelist { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<SysApplicationSecret> Secrets { get; set; } = new List<SysApplicationSecret>();
}
