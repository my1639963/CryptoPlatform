using CryptoPlatform.Crypto.Abstractions;
using CryptoPlatform.Crypto.Software;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CryptoPlatform.Infrastructure.Tests;

/// <summary>
/// CryptoProviderRouter 健康状态跟踪测试。
/// </summary>
public class CryptoProviderRouterHealthTests
{
    private readonly CryptoProviderRouter _sut;
    private readonly Mock<ICryptoProvider> _providerMock;

    public CryptoProviderRouterHealthTests()
    {
        _providerMock = new Mock<ICryptoProvider>();
        _providerMock.Setup(p => p.ProviderType).Returns("SOFTWARE");
        _providerMock.Setup(p => p.ProviderName).Returns("SoftwareProvider");

        _sut = new CryptoProviderRouter(
            new[] { _providerMock.Object },
            NullLogger<CryptoProviderRouter>.Instance);
    }

    [Fact]
    public void GetHealthStatus_InitialState_ShouldBeHealthy()
    {
        var status = _sut.GetHealthStatus("SOFTWARE");
        status.Should().Be("HEALTHY");
    }

    [Fact]
    public void ReportHealth_Healthy_ShouldResetToHealthy()
    {
        // 先制造几次失败
        _sut.ReportHealth("SOFTWARE", false);
        _sut.ReportHealth("SOFTWARE", false);
        _sut.GetHealthStatus("SOFTWARE").Should().Be("HEALTHY"); // 2 次还不够 DEGRADED

        // 恢复
        _sut.ReportHealth("SOFTWARE", true);
        _sut.GetHealthStatus("SOFTWARE").Should().Be("HEALTHY");
    }

    [Fact]
    public void ReportHealth_3Failures_ShouldBecomeDegraded()
    {
        _sut.ReportHealth("SOFTWARE", false);
        _sut.ReportHealth("SOFTWARE", false);
        _sut.ReportHealth("SOFTWARE", false);

        _sut.GetHealthStatus("SOFTWARE").Should().Be("DEGRADED");
    }

    [Fact]
    public void ReportHealth_5Failures_ShouldBecomeOffline()
    {
        for (int i = 0; i < 5; i++)
            _sut.ReportHealth("SOFTWARE", false);

        _sut.GetHealthStatus("SOFTWARE").Should().Be("OFFLINE");
    }

    [Fact]
    public void ReportHealth_RecoveryAfterOffline_ShouldReturnToHealthy()
    {
        for (int i = 0; i < 5; i++)
            _sut.ReportHealth("SOFTWARE", false);

        _sut.GetHealthStatus("SOFTWARE").Should().Be("OFFLINE");

        // 恢复
        _sut.ReportHealth("SOFTWARE", true);
        _sut.GetHealthStatus("SOFTWARE").Should().Be("HEALTHY");
    }

    [Fact]
    public void GetDefaultProvider_WhenOffline_ShouldStillReturnProvider()
    {
        for (int i = 0; i < 5; i++)
            _sut.ReportHealth("SOFTWARE", false);

        // 即使 OFFLINE，只有一个 Provider 时仍返回
        var provider = _sut.GetDefaultProvider();
        provider.Should().NotBeNull();
        provider.ProviderType.Should().Be("SOFTWARE");
    }

    [Fact]
    public void GetHealthStatus_UnknownProvider_ShouldReturnUNKNOWN()
    {
        var status = _sut.GetHealthStatus("NONEXISTENT");
        status.Should().Be("UNKNOWN");
    }

    [Fact]
    public void GetAllProviders_ShouldReturnRegisteredProviders()
    {
        var providers = _sut.GetAllProviders();
        providers.Should().HaveCount(1);
        providers[0].ProviderType.Should().Be("SOFTWARE");
    }

    [Fact]
    public void ResolveProvider_ExistingType_ShouldReturnProvider()
    {
        var provider = _sut.ResolveProvider("SOFTWARE");
        provider.Should().NotBeNull();
    }

    [Fact]
    public void ResolveProvider_NonExistentType_ShouldThrow()
    {
        var act = () => _sut.ResolveProvider("HSM");
        act.Should().Throw<CryptoProviderException>();
    }
}
