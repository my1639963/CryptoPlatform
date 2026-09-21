using System.Reflection;
using CryptoPlatform.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CryptoPlatform.Api.Tests;

/// <summary>
/// API 路由定义测试。通过反射验证所有 21 个端点的路由是否正确定义。
/// </summary>
public class ApiRouteDefinitionTests
{
    // ─── AdminAuthController ───

    [Fact]
    public void AdminAuthController_ShouldHaveCorrectRoute()
    {
        var route = GetControllerRoute<AdminAuthController>();
        route.Should().Be("api/v1/admin/auth");
    }

    [Fact]
    public void AdminAuthController_ShouldHaveLoginEndpoint()
    {
        var method = FindAction<AdminAuthController>("Login");
        method.Should().NotBeNull();
        GetHttpPostRoute(method!).Should().Be("token");
    }

    [Fact]
    public void AdminAuthController_ShouldHaveRevokeEndpoint()
    {
        var method = FindAction<AdminAuthController>("RevokeToken");
        method.Should().NotBeNull();
        GetHttpPostRoute(method!).Should().Be("token/revoke");
    }

    // ─── KeyManagementController ───

    [Fact]
    public void KeyManagementController_ShouldHaveCorrectRoute()
    {
        var route = GetControllerRoute<KeyManagementController>();
        route.Should().Be("api/v1/admin/keys");
    }

    [Theory]
    [InlineData("Create", "")]
    [InlineData("Get", "{keyId}")]
    [InlineData("Activate", "{keyId}/activate")]
    [InlineData("Rotate", "{keyId}/rotate")]
    [InlineData("Disable", "{keyId}/disable")]
    [InlineData("Revoke", "{keyId}/revoke")]
    [InlineData("Destroy", "{keyId}/destroy")]
    [InlineData("GetVersions", "{keyId}/versions")]
    [InlineData("Import", "import")]
    public void KeyManagementController_ShouldHaveEndpoints(string actionName, string expectedRoute)
    {
        var method = FindAction<KeyManagementController>(actionName);
        method.Should().NotBeNull($"Action {actionName} 应存在");

        var httpAttr = method!.GetCustomAttribute<HttpPostAttribute>();
        var httpGetAttr = method.GetCustomAttribute<HttpGetAttribute>();

        if (httpAttr is not null)
        {
            // [HttpPost] 无参数时 Template 为 null，等价于空字符串
            if (expectedRoute == "")
                httpAttr.Template.Should().BeNullOrEmpty();
            else
                httpAttr.Template.Should().Be(expectedRoute);
        }
        else if (httpGetAttr is not null)
        {
            if (expectedRoute == "")
                httpGetAttr.Template.Should().BeNullOrEmpty();
            else
                httpGetAttr.Template.Should().Be(expectedRoute);
        }
    }

    // ─── CryptoController ───

    [Fact]
    public void CryptoController_ShouldHaveCorrectRoute()
    {
        var route = GetControllerRoute<CryptoController>();
        route.Should().Be("api/v1/crypto");
    }

    [Theory]
    [InlineData("Sm4Encrypt", "sm4/encrypt")]
    [InlineData("Sm4Decrypt", "sm4/decrypt")]
    [InlineData("Sm2Encrypt", "sm2/encrypt")]
    [InlineData("Sm2Decrypt", "sm2/decrypt")]
    [InlineData("Sm2Sign", "sm2/sign")]
    [InlineData("Sm2Verify", "sm2/verify")]
    [InlineData("Sm3Hash", "sm3/hash")]
    [InlineData("HmacGenerate", "hmac/generate")]
    [InlineData("HmacVerify", "hmac/verify")]
    [InlineData("Random", "random")]
    public void CryptoController_ShouldHaveEndpoints(string actionName, string expectedRoute)
    {
        var method = FindAction<CryptoController>(actionName);
        method.Should().NotBeNull($"Action {actionName} 应存在");

        var httpAttr = method!.GetCustomAttribute<HttpPostAttribute>();
        httpAttr.Should().NotBeNull($"{actionName} 应为 POST 方法");
        httpAttr!.Template.Should().Be(expectedRoute);
    }

    // ─── 端点总数验证 ───

    [Fact]
    public void AllControllers_ShouldHave21Endpoints()
    {
        var endpointCount = 0;

        // AdminAuthController: 2 个端点
        endpointCount += CountActions<AdminAuthController>();
        // KeyManagementController: 9 个端点
        endpointCount += CountActions<KeyManagementController>();
        // CryptoController: 10 个端点
        endpointCount += CountActions<CryptoController>();

        endpointCount.Should().Be(21, "应共有 21 个 API 端点");
    }

    // ─── 辅助方法 ───

    private static string GetControllerRoute<TController>()
    {
        var attr = typeof(TController).GetCustomAttribute<RouteAttribute>();
        return attr?.Template ?? "";
    }

    private static MethodInfo? FindAction<TController>(string actionName)
    {
        return typeof(TController).GetMethod(actionName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    private static string? GetHttpPostRoute(MethodInfo method)
    {
        return method.GetCustomAttribute<HttpPostAttribute>()?.Template;
    }

    private static int CountActions<TController>()
    {
        return typeof(TController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Count(m => m.GetCustomAttribute<HttpPostAttribute>() is not null
                     || m.GetCustomAttribute<HttpGetAttribute>() is not null);
    }
}
