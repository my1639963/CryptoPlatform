using System.Text;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Infrastructure.Caching;
using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Digests;

namespace CryptoPlatform.Audit;

/// <summary>
/// SM3 哈希链服务接口。
/// 确保审计日志追加写入、不可篡改。
/// 算法：SM3(Log[n] + Hash[n-1]) = Hash[n]
/// </summary>
public interface IHashChainService
{
    /// <summary>计算审计日志条目的哈希值</summary>
    string ComputeHash(AuditLogEntry entry, string? previousHash);

    /// <summary>获取链中最新的哈希值</summary>
    Task<string?> GetLatestHashAsync(CancellationToken ct);

    /// <summary>更新最新哈希值到缓存</summary>
    Task UpdateLatestHashAsync(string hash, CancellationToken ct);

    /// <summary>验证指定时间范围内的哈希链完整性</summary>
    Task<bool> VerifyChainAsync(DateTime from, DateTime to, CancellationToken ct);
}

public sealed class HashChainService : IHashChainService
{
    private readonly CryptoPlatformDbContext _db;
    private readonly ICacheService _cache;
    private const string LatestHashKey = "audit:chain:latest_hash";

    public HashChainService(CryptoPlatformDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>
    /// 计算审计日志条目的 SM3 哈希。
    /// 输入：日志关键字段拼接 + 前一条哈希值。
    /// </summary>
    public string ComputeHash(AuditLogEntry entry, string? previousHash)
    {
        var content = $"{entry.AuditId}|{entry.Timestamp:O}|{entry.OperatorType}|{entry.OperatorId}"
                    + $"|{entry.AppId}|{entry.Operation}|{entry.KeyId}|{entry.ResultCode}|{previousHash ?? ""}";
        var bytes = Encoding.UTF8.GetBytes(content);

        // 使用 SM3 杂凑算法
        var digest = new SM3Digest();
        digest.BlockUpdate(bytes, 0, bytes.Length);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);

        return Convert.ToHexString(hash);
    }

    public async Task<string?> GetLatestHashAsync(CancellationToken ct)
    {
        // 优先从缓存读取
        var cached = await _cache.GetStringAsync(LatestHashKey);
        if (cached is not null) return cached;

        // 缓存未命中，从数据库最后一条记录获取
        var lastLog = await _db.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefaultAsync(ct);

        return lastLog?.CurrentHash;
    }

    public async Task UpdateLatestHashAsync(string hash, CancellationToken ct)
    {
        await _cache.SetStringSlidingAsync(LatestHashKey, hash, TimeSpan.FromDays(30), ct);
    }

    /// <summary>
    /// 验证指定时间范围内审计日志的哈希链完整性。
    /// 从第一条记录的 PreviousHash 开始，逐条重新计算并比对。
    /// </summary>
    public async Task<bool> VerifyChainAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var logs = await _db.AuditLogs
            .Where(l => l.Timestamp >= from && l.Timestamp <= to)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(ct);

        if (logs.Count == 0) return true;

        string? expectedPrev = logs[0].PreviousHash;

        foreach (var log in logs)
        {
            var entry = new AuditLogEntry(
                log.AuditId, log.RequestId, log.TraceId, log.Timestamp,
                log.OperatorType, log.OperatorId, log.OperatorName,
                log.AppId, log.SourceIp, log.Operation,
                log.KeyId, log.KeyVersion, log.ResultCode, log.DurationMs);

            var computed = ComputeHash(entry, expectedPrev);
            if (!string.Equals(computed, log.CurrentHash, StringComparison.Ordinal))
                return false;

            expectedPrev = log.CurrentHash;
        }

        return true;
    }
}
