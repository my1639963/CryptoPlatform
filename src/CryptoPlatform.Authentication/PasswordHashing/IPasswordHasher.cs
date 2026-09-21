namespace CryptoPlatform.Authentication.PasswordHashing;

/// <summary>
/// 密码哈希器接口。
/// 每种 KDF 算法对应一个实现，支持哈希生成、验证和升级判断。
/// 存储格式：{algorithm}${version}${base64salt}${iterations}${base64hash}
/// </summary>
public interface IPasswordHasher
{
    /// <summary>算法标识，例如 PBKDF2-SHA256</summary>
    string Algorithm { get; }

    /// <summary>当前算法版本号</summary>
    int Version { get; }

    /// <summary>
    /// 对明文密码进行哈希。
    /// 返回格式：{algorithm}${version}${base64salt}${iterations}${base64hash}
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// 验证明文密码与已存储的哈希字符串是否匹配。
    /// </summary>
    bool VerifyPassword(string password, string formattedHash);

    /// <summary>
    /// 判断已存储的哈希字符串是否需要升级到当前默认算法/版本。
    /// 返回 true 表示应使用当前默认 Hasher 重新哈希。
    /// </summary>
    bool NeedsUpgrade(string formattedHash);
}
