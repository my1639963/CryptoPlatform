using CryptoPlatform.Domain;
using CryptoPlatform.Domain.Enums;
using FluentAssertions;

namespace CryptoPlatform.Domain.Tests;

/// <summary>
/// 密钥生命周期状态转换验证器单元测试。
/// 覆盖需求文档 3.2.4 定义的全部 14 条允许转换 + 4 条显式禁止转换。
/// </summary>
public class KeyStatusTransitionTests
{
    // ── 14 条允许的状态转换 ──

    [Theory]
    [InlineData(KeyStatus.CREATED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.CREATED, KeyStatus.REVOKED)]
    [InlineData(KeyStatus.CREATED, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.ROTATED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.DISABLED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.EXPIRED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.REVOKED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.DISABLED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.DISABLED, KeyStatus.REVOKED)]
    [InlineData(KeyStatus.DISABLED, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.ROTATED, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.EXPIRED, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.REVOKED, KeyStatus.DESTROYED)]
    public void AllowedTransitions_ShouldReturnTrue(KeyStatus from, KeyStatus to)
    {
        // Act
        var isAllowed = KeyStatusTransition.IsAllowed(from, to);

        // Assert
        isAllowed.Should().BeTrue($"从 {from} 到 {to} 的转换应当被允许");
    }

    [Theory]
    [InlineData(KeyStatus.CREATED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.CREATED, KeyStatus.REVOKED)]
    [InlineData(KeyStatus.CREATED, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.ROTATED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.DISABLED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.EXPIRED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.REVOKED)]
    [InlineData(KeyStatus.ACTIVE, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.DISABLED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.DISABLED, KeyStatus.REVOKED)]
    [InlineData(KeyStatus.DISABLED, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.ROTATED, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.EXPIRED, KeyStatus.DESTROYED)]
    [InlineData(KeyStatus.REVOKED, KeyStatus.DESTROYED)]
    public void AllowedTransitions_Validate_ShouldNotThrow(KeyStatus from, KeyStatus to)
    {
        // Act
        var action = () => KeyStatusTransition.Validate(from, to);

        // Assert
        action.Should().NotThrow();
    }

    // ── 4 条显式禁止的状态转换 ──

    [Theory]
    [InlineData(KeyStatus.DESTROYED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.DESTROYED, KeyStatus.CREATED)]
    [InlineData(KeyStatus.DESTROYED, KeyStatus.REVOKED)]
    [InlineData(KeyStatus.REVOKED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.EXPIRED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.ROTATED, KeyStatus.ACTIVE)]
    public void ForbiddenTransitions_ShouldReturnFalse(KeyStatus from, KeyStatus to)
    {
        // Act
        var isAllowed = KeyStatusTransition.IsAllowed(from, to);

        // Assert
        isAllowed.Should().BeFalse($"从 {from} 到 {to} 的转换应当被禁止");
    }

    [Theory]
    [InlineData(KeyStatus.DESTROYED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.REVOKED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.EXPIRED, KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.ROTATED, KeyStatus.ACTIVE)]
    public void ForbiddenTransitions_Validate_ShouldThrow(KeyStatus from, KeyStatus to)
    {
        // Act
        var action = () => KeyStatusTransition.Validate(from, to);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{from}*{to}*");
    }

    // ── DESTROYED 是终态，不允许转到任何状态 ──

    [Theory]
    [InlineData(KeyStatus.CREATED)]
    [InlineData(KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.ROTATED)]
    [InlineData(KeyStatus.DISABLED)]
    [InlineData(KeyStatus.EXPIRED)]
    [InlineData(KeyStatus.REVOKED)]
    [InlineData(KeyStatus.DESTROYED)]
    public void DestroyedIsTerminalState_ShouldNotAllowAnyTransition(KeyStatus to)
    {
        KeyStatusTransition.IsAllowed(KeyStatus.DESTROYED, to)
            .Should().BeFalse("DESTROYED 是终态，不允许转到任何状态");
    }

    // ── 自转换不允许 ──

    [Theory]
    [InlineData(KeyStatus.CREATED)]
    [InlineData(KeyStatus.ACTIVE)]
    [InlineData(KeyStatus.ROTATED)]
    [InlineData(KeyStatus.DISABLED)]
    [InlineData(KeyStatus.EXPIRED)]
    [InlineData(KeyStatus.REVOKED)]
    [InlineData(KeyStatus.DESTROYED)]
    public void SelfTransition_ShouldNotBeAllowed(KeyStatus status)
    {
        KeyStatusTransition.IsAllowed(status, status)
            .Should().BeFalse($"状态 {status} 不允许自转换");
    }

    // ── GetAllowedTargets 测试 ──

    [Fact]
    public void GetAllowedTargets_Created_ShouldReturn3Targets()
    {
        var targets = KeyStatusTransition.GetAllowedTargets(KeyStatus.CREATED);
        targets.Should().HaveCount(3);
        targets.Should().Contain(KeyStatus.ACTIVE);
        targets.Should().Contain(KeyStatus.REVOKED);
        targets.Should().Contain(KeyStatus.DESTROYED);
    }

    [Fact]
    public void GetAllowedTargets_Active_ShouldReturn5Targets()
    {
        var targets = KeyStatusTransition.GetAllowedTargets(KeyStatus.ACTIVE);
        targets.Should().HaveCount(5);
        targets.Should().Contain(KeyStatus.ROTATED);
        targets.Should().Contain(KeyStatus.DISABLED);
        targets.Should().Contain(KeyStatus.EXPIRED);
        targets.Should().Contain(KeyStatus.REVOKED);
        targets.Should().Contain(KeyStatus.DESTROYED);
    }

    [Fact]
    public void GetAllowedTargets_Destroyed_ShouldReturnEmpty()
    {
        var targets = KeyStatusTransition.GetAllowedTargets(KeyStatus.DESTROYED);
        targets.Should().BeEmpty();
    }

    // ── GetCondition 测试 ──

    [Fact]
    public void GetCondition_AllowedTransition_ShouldReturnDescription()
    {
        var condition = KeyStatusTransition.GetCondition(KeyStatus.ACTIVE, KeyStatus.ROTATED);
        condition.Should().NotBeNullOrEmpty();
        condition.Should().Be("新版本产生");
    }

    [Fact]
    public void GetCondition_DisallowedTransition_ShouldReturnNA()
    {
        var condition = KeyStatusTransition.GetCondition(KeyStatus.DESTROYED, KeyStatus.ACTIVE);
        condition.Should().Be("N/A");
    }

    // ── 总计 14 条允许转换验证 ──

    [Fact]
    public void TotalAllowedTransitions_ShouldBe14()
    {
        var allStatuses = Enum.GetValues<KeyStatus>();
        var allowedCount = 0;

        foreach (var from in allStatuses)
        {
            foreach (var to in allStatuses)
            {
                if (KeyStatusTransition.IsAllowed(from, to))
                    allowedCount++;
            }
        }

        allowedCount.Should().Be(14, "状态转换矩阵应恰好包含 14 条允许转换");
    }
}
