using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CryptoPlatform.Authentication;

/// <summary>
/// 令牌生成器接口。
/// </summary>
public interface ITokenGenerator
{
    /// <summary>生成 JWT 令牌</summary>
    string Generate(TokenPayload payload);

    /// <summary>验证并解析 JWT 令牌</summary>
    TokenPayload? Validate(string token);
}

/// <summary>
/// 基于 System.IdentityModel.Tokens.Jwt 的 JWT 令牌生成器。
/// </summary>
public sealed class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly ILogger<JwtTokenGenerator> _logger;

    public JwtTokenGenerator(JwtSettings settings, ILogger<JwtTokenGenerator> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public string Generate(TokenPayload payload)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, payload.Jti),
            new Claim(JwtRegisteredClaimNames.Sub, payload.Subject),
            new Claim(JwtRegisteredClaimNames.Iss, payload.Issuer),
            new Claim(JwtRegisteredClaimNames.Aud, payload.Audience),
            new Claim("operator_id", payload.OperatorId),
            new Claim("role", payload.Role),
            new Claim("scopes", string.Join(",", payload.Scopes))
        };

        var token = new JwtSecurityToken(
            issuer: payload.Issuer,
            audience: payload.Audience,
            claims: claims,
            expires: payload.Expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public TokenPayload? Validate(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
            var handler = new JwtSecurityTokenHandler();
            // 禁用默认 claim 名称映射，保留原始 claim 名称
            handler.MapInboundClaims = false;
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            var jwtToken = handler.ReadJwtToken(token);

            var scopesClaim = principal.FindFirst("scopes")?.Value ?? "";
            var scopes = scopesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            return new TokenPayload(
                Jti: jwtToken.Id ?? "",
                Issuer: jwtToken.Issuer,
                Subject: jwtToken.Subject,
                Audience: jwtToken.Audiences.FirstOrDefault() ?? "",
                OperatorId: principal.FindFirst("operator_id")?.Value ?? "",
                Role: principal.FindFirst("role")?.Value ?? "",
                Scopes: scopes,
                Expires: jwtToken.ValidTo);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("JWT 验证失败: {Error}", ex.Message);
            return null;
        }
    }
}
