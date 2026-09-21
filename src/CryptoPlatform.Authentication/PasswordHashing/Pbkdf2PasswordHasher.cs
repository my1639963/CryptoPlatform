using System.Security.Cryptography;

namespace CryptoPlatform.Authentication.PasswordHashing;

/// <summary>
/// 基于 PBKDF2 + SHA256 的密码哈希器（默认实现）。
/// 参数：16 字节随机 Salt，600,000 次迭代（OWASP 2023 推荐最低值），32 字节子密钥。
/// 存储格式：PBKDF2-SHA256$1${base64salt}${iterations}${base64hash}
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    /// <summary>Salt 长度（字节）</summary>
    private const int SaltSize = 16;

    /// <summary>子密钥长度（字节）</summary>
    private const int HashSize = 32;

    /// <summary>OWASP 2023 推荐最低迭代次数</summary>
    private const int Iterations = 600_000;

    /// <summary>字段分隔符</summary>
    private const char Separator = '$';

    public string Algorithm => "PBKDF2-SHA256";
    public int Version => 1;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputePbkdf2(password, salt, Iterations);

        // 格式：PBKDF2-SHA256$1${base64salt}${iterations}${base64hash}
        return string.Join(Separator,
            Algorithm,
            Version.ToString(),
            Convert.ToBase64String(salt),
            Iterations.ToString(),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string password, string formattedHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(formattedHash);

        if (!TryParse(formattedHash, out var storedAlgorithm, out _, out var saltB64, out var iterStr, out var hashB64))
            return false;

        if (!string.Equals(storedAlgorithm, Algorithm, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!int.TryParse(iterStr, out var iterations) || iterations <= 0)
            return false;

        var salt = Convert.FromBase64String(saltB64);
        var expectedHash = Convert.FromBase64String(hashB64);
        var actualHash = ComputePbkdf2(password, salt, iterations);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public bool NeedsUpgrade(string formattedHash)
    {
        if (!TryParse(formattedHash, out var storedAlgorithm, out var versionStr, out _, out var iterStr, out _))
            return true; // 无法解析，需要升级

        if (!string.Equals(storedAlgorithm, Algorithm, StringComparison.OrdinalIgnoreCase))
            return true; // 算法不同，需要升级

        if (!int.TryParse(versionStr, out var version) || version < Version)
            return true; // 版本低于当前版本

        if (!int.TryParse(iterStr, out var iterations) || iterations < Iterations)
            return true; // 迭代次数不足

        return false;
    }

    /// <summary>
    /// 解析哈希字符串。
    /// 格式：{algorithm}${version}${base64salt}${iterations}${base64hash}
    /// </summary>
    private static bool TryParse(
        string formattedHash,
        out string algorithm,
        out string version,
        out string salt,
        out string iterations,
        out string hash)
    {
        algorithm = string.Empty;
        version = string.Empty;
        salt = string.Empty;
        iterations = string.Empty;
        hash = string.Empty;

        var parts = formattedHash.Split(Separator);
        if (parts.Length != 5)
            return false;

        algorithm = parts[0];
        version = parts[1];
        salt = parts[2];
        iterations = parts[3];
        hash = parts[4];
        return true;
    }

    private static byte[] ComputePbkdf2(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashSize);
    }
}
