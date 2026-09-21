using CryptoPlatform.Infrastructure.Locking;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CryptoPlatform.Infrastructure.Tests;

/// <summary>
/// InMemoryDistributedLock 单元测试。
/// </summary>
public class InMemoryDistributedLockTests
{
    private readonly InMemoryDistributedLock _sut;

    public InMemoryDistributedLockTests()
    {
        _sut = new InMemoryDistributedLock(NullLogger<InMemoryDistributedLock>.Instance);
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldSucceed_WhenLockNotHeld()
    {
        var handle = await _sut.TryAcquireAsync("test:lock:1", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        handle.Should().NotBeNull();
        await handle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldFail_WhenLockHeld()
    {
        var handle1 = await _sut.TryAcquireAsync("test:lock:2", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        handle1.Should().NotBeNull();

        // 第二次获取同一把锁，不等待 → 应返回 null
        var handle2 = await _sut.TryAcquireAsync("test:lock:2", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        handle2.Should().BeNull();

        await handle1!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldSucceed_AfterRelease()
    {
        var handle1 = await _sut.TryAcquireAsync("test:lock:3", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        handle1.Should().NotBeNull();
        await handle1!.DisposeAsync();

        // 释放后应能重新获取
        var handle2 = await _sut.TryAcquireAsync("test:lock:3", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        handle2.Should().NotBeNull();
        await handle2!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_DifferentKeys_ShouldNotConflict()
    {
        var handle1 = await _sut.TryAcquireAsync("test:lock:a", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        var handle2 = await _sut.TryAcquireAsync("test:lock:b", TimeSpan.FromSeconds(10), TimeSpan.Zero);

        handle1.Should().NotBeNull();
        handle2.Should().NotBeNull();

        await handle1!.DisposeAsync();
        await handle2!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldWaitAndSucceed_WithTimeout()
    {
        var handle1 = await _sut.TryAcquireAsync("test:lock:4", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        handle1.Should().NotBeNull();

        // 在后台释放锁
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            await handle1.DisposeAsync();
        });

        // 等待最多 5 秒获取锁
        var handle2 = await _sut.TryAcquireAsync("test:lock:4", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));
        handle2.Should().NotBeNull("应在超时前获取到锁");
        await handle2!.DisposeAsync();
    }

    [Fact]
    public async Task LockHandle_ShouldBeDisposable()
    {
        var handle = await _sut.TryAcquireAsync("test:lock:5", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        handle.Should().NotBeNull();
        // 释放不应抛异常
        await handle!.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldBeIdempotent()
    {
        var handle = await _sut.TryAcquireAsync("test:lock:6", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        handle.Should().NotBeNull();

        // 多次释放不应抛异常
        await handle!.DisposeAsync();
        await handle.DisposeAsync();
        await handle.DisposeAsync();
    }
}
