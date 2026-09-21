using CryptoPlatform.Audit;
using CryptoPlatform.Infrastructure.Caching;
using FluentAssertions;

namespace CryptoPlatform.Phase4.Tests;

/// <summary>
/// HashChainService 单元测试。
/// </summary>
public class HashChainServiceTests
{
    private readonly HashChainService _sut;
    private readonly CryptoPlatform.Persistence.CryptoPlatformDbContext _db;
    private readonly ICacheService _cache;

    public HashChainServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _cache = TestCacheFactory.Create();
        _sut = new HashChainService(_db, _cache);
    }

    [Fact]
    public void ComputeHash_ShouldReturnNonEmptyHexString()
    {
        var entry = TestDataFactory.CreateAuditEntry();
        var hash = _sut.ComputeHash(entry, null);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().MatchRegex("^[0-9A-F]+$");
        // SM3 输出 32 字节 = 64 个十六进制字符
        hash.Length.Should().Be(64);
    }

    [Fact]
    public void ComputeHash_SameInput_ShouldReturnSameHash()
    {
        var entry = TestDataFactory.CreateAuditEntry();
        var hash1 = _sut.ComputeHash(entry, null);
        var hash2 = _sut.ComputeHash(entry, null);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentPreviousHash_ShouldReturnDifferentHash()
    {
        var entry = TestDataFactory.CreateAuditEntry();
        var hash1 = _sut.ComputeHash(entry, null);
        var hash2 = _sut.ComputeHash(entry, "PREVIOUS_HASH_VALUE");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentEntries_ShouldReturnDifferentHash()
    {
        var entry1 = TestDataFactory.CreateAuditEntry(operation: "OP_A");
        var entry2 = TestDataFactory.CreateAuditEntry(operation: "OP_B");

        var hash1 = _sut.ComputeHash(entry1, null);
        var hash2 = _sut.ComputeHash(entry2, null);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task GetLatestHashAsync_EmptyDb_ShouldReturnNull()
    {
        var hash = await _sut.GetLatestHashAsync(CancellationToken.None);
        hash.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLatestHashAsync_ShouldStoreInCache()
    {
        await _sut.UpdateLatestHashAsync("TEST_HASH", CancellationToken.None);
        var hash = await _sut.GetLatestHashAsync(CancellationToken.None);
        hash.Should().Be("TEST_HASH");
    }

    [Fact]
    public async Task VerifyChainAsync_EmptyRange_ShouldReturnTrue()
    {
        var result = await _sut.VerifyChainAsync(
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, CancellationToken.None);
        result.Should().BeTrue();
    }
}
