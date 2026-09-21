using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace CryptoPlatform.Authentication.PasswordHashing;

/// <summary>
/// 基于 SM3 + 迭代拉伸结构的密码哈希器（合规备选实现）。
/// SM3 本身是快速杂凑，不能直接作为密码存储 KDF；
/// 本实现通过 HMAC-SM3 迭代拉伸（类似 PBKDF2 结构）构造 KDF。
/// 存储格式：PBKDF2-SM3$1${base64salt}${iterations}${base64hash}
/// 注意：仅在有明确合规依据时启用，默认不注册。
/// </summary>
public sealed class Sm3PasswordHasher : IPasswordHasher
{
    /// <summary>Salt 长度（字节）</summary>
    private const int SaltSize = 16;

    /// <summary>子密钥长度（字节），SM3 输出 32 字节</summary>
    private const int HashSize = 32;

    /// <summary>迭代次数（商密场景建议不低于 100,000）</summary>
    private const int Iterations = 100_000;

    private const char Separator = '$';

    public string Algorithm => "PBKDF2-SM3";
    public int Version => 1;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputePbkdf2Sm3(password, salt, Iterations);

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
        var actualHash = ComputePbkdf2Sm3(password, salt, iterations);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public bool NeedsUpgrade(string formattedHash)
    {
        if (!TryParse(formattedHash, out var storedAlgorithm, out var versionStr, out _, out var iterStr, out _))
            return true;

        if (!string.Equals(storedAlgorithm, Algorithm, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!int.TryParse(versionStr, out var version) || version < Version)
            return true;

        if (!int.TryParse(iterStr, out var iterations) || iterations < Iterations)
            return true;

        return false;
    }

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
        if (parts.Length != 5) return false;

        algorithm = parts[0];
        version = parts[1];
        salt = parts[2];
        iterations = parts[3];
        hash = parts[4];
        return true;
    }

    /// <summary>
    /// 使用 BouncyCastle 实现 PBKDF2-HMAC-SM3。
    /// 基于 PKCS5S2ParametersGenerator + SM3Digest 构造标准 PBKDF2 结构。
    /// </summary>
    private static byte[] ComputePbkdf2Sm3(string password, byte[] salt, int iterations)
    {
        var generator = new Pkcs5S2ParametersGenerator(new SM3Digest());
        generator.Init(
            System.Text.Encoding.UTF8.GetBytes(password),
            salt,
            iterations);
        var key = (KeyParameter)generator.GenerateDerivedMacParameters(HashSize * 8);
        return key.GetKey();
    }
}
