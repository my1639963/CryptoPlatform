using CryptoPlatform.Crypto.Abstractions;
using CryptoPlatform.Crypto.Software;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CryptoPlatform.Crypto.Software.Tests;

public class CryptoProviderRouterTests
{
    [Fact]
    public void GetDefaultProvider_ReturnsSoftwareProvider()
    {
        var provider = new SoftwareCryptoProvider(NullLogger<SoftwareCryptoProvider>.Instance);
        var router = new CryptoProviderRouter(new[] { provider }, NullLogger<CryptoProviderRouter>.Instance);

        var defaultProvider = router.GetDefaultProvider();

        defaultProvider.Should().Be(provider);
        defaultProvider.ProviderType.Should().Be("SOFTWARE");
    }

    [Fact]
    public void ResolveProvider_SOFTWARE_ReturnsProvider()
    {
        var provider = new SoftwareCryptoProvider(NullLogger<SoftwareCryptoProvider>.Instance);
        var router = new CryptoProviderRouter(new[] { provider }, NullLogger<CryptoProviderRouter>.Instance);

        var resolved = router.ResolveProvider("SOFTWARE");

        resolved.Should().Be(provider);
    }

    [Fact]
    public void ResolveProvider_UnknownType_Throws()
    {
        var provider = new SoftwareCryptoProvider(NullLogger<SoftwareCryptoProvider>.Instance);
        var router = new CryptoProviderRouter(new[] { provider }, NullLogger<CryptoProviderRouter>.Instance);

        var act = () => router.ResolveProvider("HSM");

        act.Should().Throw<CryptoProviderException>()
            .Where(e => e.ErrorCode == ProviderErrorCodes.PROVIDER_UNAVAILABLE);
    }

    [Fact]
    public void GetDefaultProvider_NoProviders_Throws()
    {
        var router = new CryptoProviderRouter(Array.Empty<ICryptoProvider>(), NullLogger<CryptoProviderRouter>.Instance);

        var act = () => router.GetDefaultProvider();

        act.Should().Throw<CryptoProviderException>()
            .Where(e => e.ErrorCode == ProviderErrorCodes.PROVIDER_UNAVAILABLE);
    }

    [Fact]
    public void GetAllProviders_ReturnsAll()
    {
        var provider = new SoftwareCryptoProvider(NullLogger<SoftwareCryptoProvider>.Instance);
        var router = new CryptoProviderRouter(new[] { provider }, NullLogger<CryptoProviderRouter>.Instance);

        var all = router.GetAllProviders();

        all.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveByDevice_ReturnsDefault()
    {
        var provider = new SoftwareCryptoProvider(NullLogger<SoftwareCryptoProvider>.Instance);
        var router = new CryptoProviderRouter(new[] { provider }, NullLogger<CryptoProviderRouter>.Instance);

        var resolved = router.ResolveByDevice("any-device-id");

        resolved.Should().Be(provider);
    }
}
