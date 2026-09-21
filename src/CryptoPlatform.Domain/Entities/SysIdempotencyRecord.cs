namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 幂等记录。用于防止重复提交。
/// </summary>
public sealed class SysIdempotencyRecord
{
    public long Id { get; set; }
    public string AppId { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string RequestPath { get; set; } = null!;
    public string RequestHash { get; set; } = null!;
    public int? ResponseCode { get; set; }
    public string? ResponseBodyHash { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
