using Microsoft.AspNetCore.Mvc;

namespace CryptoPlatform.Api.Controllers;

/// <summary>
/// 健康检查控制器。用于负载均衡器和监控系统检测服务状态。
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "Healthy",
        timestamp = DateTimeOffset.UtcNow.ToString("o"),
        service = "CryptoPlatform"
    });
}
