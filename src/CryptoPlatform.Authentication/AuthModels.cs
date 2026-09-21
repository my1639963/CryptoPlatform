namespace CryptoPlatform.Authentication;

/// <summary>JWT 配置</summary>
public sealed class JwtSettings
{
    public string Issuer { get; set; } = "CryptoPlatform";
    public string Audience { get; set; } = "management-api";
    public int ExpiresInSeconds { get; set; } = 7200; // 2 小时
    public string SigningKey { get; set; } = "CryptoPlatform.DefaultJwtSigningKey!2026";
}

/// <summary>管理员身份主体</summary>
public sealed record AdminPrincipal(
    string OperatorId,
    string Username,
    string Role,
    IReadOnlyList<string> Scopes);

/// <summary>管理员登录响应</summary>
public sealed record AdminLoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string OperatorId,
    string Role);

/// <summary>令牌载荷</summary>
public sealed record TokenPayload(
    string Jti,
    string Issuer,
    string Subject,
    string Audience,
    string OperatorId,
    string Role,
    IReadOnlyList<string> Scopes,
    DateTime Expires);

/// <summary>应用认证结果</summary>
public sealed record AppAuthResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);
