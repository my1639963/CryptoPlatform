using CryptoPlatform.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CryptoPlatform.Infrastructure.Tests;

/// <summary>
/// DistributedCacheService 单元测试。
/// 使用 MemoryDistributedCache 作为底层实现。
/// </summary>
public class DistributedCacheServiceTests
{
    private readonly ICacheService _sut;

    public DistributedCacheServiceTests()
    {
        var options = Options.Create(new MemoryDistributedCacheOptions());
        var cache = new MemoryDistributedCache(options);
        _sut = new DistributedCacheService(cache);
    }

    [Fact]
    public async Task GetStringAsync_NonExistentKey_ShouldReturnNull()
    {
        var result = await _sut.GetStringAsync("nonexistent", TestContext.Current.CancellationToken);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetStringAsync_ShouldBeRetrievable()
    {
        await _sut.SetStringAsync("test:key", "test-value", TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        var result = await _sut.GetStringAsync("test:key", TestContext.Current.CancellationToken);
        result.Should().Be("test-value");
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey()
    {
        await _sut.SetStringAsync("test:remove", "value", TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        await _sut.RemoveAsync("test:remove", TestContext.Current.CancellationToken);
        var result = await _sut.GetStringAsync("test:remove", TestContext.Current.CancellationToken);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ShouldReturnTrue()
    {
        await _sut.SetStringAsync("test:exists", "value", TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        var result = await _sut.ExistsAsync("test:exists", TestContext.Current.CancellationToken);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentKey_ShouldReturnFalse()
    {
        var result = await _sut.ExistsAsync("test:not-exists", TestContext.Current.CancellationToken);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetIfNotExistsAsync_NewKey_ShouldReturnTrue()
    {
        var result = await _sut.SetIfNotExistsAsync("test:nx-new", "value", TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        result.Should().BeTrue();

        // 验证值已写入
        var stored = await _sut.GetStringAsync("test:nx-new", TestContext.Current.CancellationToken);
        stored.Should().Be("value");
    }

    [Fact]
    public async Task SetIfNotExistsAsync_ExistingKey_ShouldReturnFalse()
    {
        await _sut.SetStringAsync("test:nx-existing", "original", TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        var result = await _sut.SetIfNotExistsAsync("test:nx-existing", "new-value", TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        result.Should().BeFalse();

        // 验证原值未被覆盖
        var stored = await _sut.GetStringAsync("test:nx-existing", TestContext.Current.CancellationToken);
        stored.Should().Be("original");
    }

    [Fact]
    public async Task SetStringAbsoluteAsync_ShouldWork()
    {
        var expiration = DateTimeOffset.UtcNow.AddMinutes(5);
        await _sut.SetStringAbsoluteAsync("test:absolute", "value", expiration, TestContext.Current.CancellationToken);
        var result = await _sut.GetStringAsync("test:absolute", TestContext.Current.CancellationToken);
        result.Should().Be("value");
    }

    [Fact]
    public async Task SetStringSlidingAsync_ShouldWork()
    {
        await _sut.SetStringSlidingAsync("test:sliding", "value", TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        var result = await _sut.GetStringAsync("test:sliding", TestContext.Current.CancellationToken);
        result.Should().Be("value");
    }
}
