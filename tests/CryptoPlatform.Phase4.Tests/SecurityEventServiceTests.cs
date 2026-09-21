using CryptoPlatform.Domain;
using CryptoPlatform.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CryptoPlatform.Phase4.Tests;

/// <summary>
/// SecurityEventService 单元测试。
/// </summary>
public class SecurityEventServiceTests
{
    private readonly SecurityEventService _sut;
    private readonly CryptoPlatform.Persistence.CryptoPlatformDbContext _db;

    public SecurityEventServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<SecurityEventService>>();
        _sut = new SecurityEventService(_db, logger.Object);
    }

    [Fact]
    public async Task RaiseAsync_WithKeyId_ShouldCreateEvent()
    {
        await _sut.RaiseAsync("AUTH_FAILURE", "KEY_001", "认证失败", CancellationToken.None);

        var events = await _db.SecurityEvents.ToListAsync();
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("AUTH_FAILURE");
        events[0].RelatedKeyId.Should().Be("KEY_001");
        events[0].Status.Should().Be("OPEN");
        events[0].Severity.Should().Be("MEDIUM");
    }

    [Fact]
    public async Task RaiseAsync_WithAppId_ShouldCreateEvent()
    {
        await _sut.RaiseAsync("APP_SECRET_BRUTE_FORCE", "APP_001", "KEY_001", "暴力破解", CancellationToken.None);

        var events = await _db.SecurityEvents.ToListAsync();
        events.Should().HaveCount(1);
        events[0].AppId.Should().Be("APP_001");
        events[0].RelatedKeyId.Should().Be("KEY_001");
        events[0].Severity.Should().Be("HIGH");
    }

    [Theory]
    [InlineData("HSM_OFFLINE", "CRITICAL")]
    [InlineData("AUDIT_INTEGRITY_FAILURE", "CRITICAL")]
    [InlineData("APP_SECRET_BRUTE_FORCE", "HIGH")]
    [InlineData("TOKEN_REPLAY", "HIGH")]
    [InlineData("AUTH_FAILURE", "MEDIUM")]
    [InlineData("KEY_ROTATION_REQUIRED", "LOW")]
    [InlineData("UNKNOWN_EVENT", "INFO")]
    public async Task RaiseAsync_ShouldDetermineCorrectSeverity(string eventType, string expectedSeverity)
    {
        await _sut.RaiseAsync(eventType, null, "test", CancellationToken.None);

        var events = await _db.SecurityEvents.ToListAsync();
        events.Should().HaveCount(1);
        events[0].Severity.Should().Be(expectedSeverity);
    }

    [Fact]
    public async Task GetOpenEventsAsync_ShouldReturnOpenEvents()
    {
        await _sut.RaiseAsync("AUTH_FAILURE", null, "event1", CancellationToken.None);
        await _sut.RaiseAsync("AUTH_FAILURE", null, "event2", CancellationToken.None);

        var openEvents = await _sut.GetOpenEventsAsync(10, CancellationToken.None);
        openEvents.Should().HaveCount(2);
    }

    [Fact]
    public async Task CloseAsync_ShouldUpdateEventStatus()
    {
        await _sut.RaiseAsync("AUTH_FAILURE", null, "test event", CancellationToken.None);
        var events = await _db.SecurityEvents.ToListAsync();
        var eventId = events[0].EventId;

        await _sut.CloseAsync(eventId, "admin", "已处理", CancellationToken.None);

        var closedEvent = await _db.SecurityEvents.FirstAsync(e => e.EventId == eventId);
        closedEvent.Status.Should().Be("CLOSED");
        closedEvent.Handler.Should().Be("admin");
        closedEvent.HandlingResult.Should().Be("已处理");
    }

    [Fact]
    public async Task CloseAsync_NonExistentEvent_ShouldThrow()
    {
        var act = () => _sut.CloseAsync("NON_EXISTENT", "admin", "test", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "EVENT_NOT_FOUND");
    }
}
