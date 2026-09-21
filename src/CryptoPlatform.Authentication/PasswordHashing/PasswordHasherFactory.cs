namespace CryptoPlatform.Authentication.PasswordHashing;

/// <summary>
/// 密码哈希器工厂接口。
/// 根据算法标识获取对应的 Hasher 实现，并标识当前默认 Hasher。
/// </summary>
public interface IPasswordHasherFactory
{
    /// <summary>获取当前默认密码哈希器（用于新密码哈希和升级判断）</summary>
    IPasswordHasher GetDefault();

    /// <summary>
    /// 根据已存储的算法标识和版本号获取对应的哈希器。
    /// 用于登录验证时，使用与存储时相同的算法进行校验。
    /// </summary>
    /// <returns>对应的哈希器；若找不到则返回 null，调用方应拒绝验证。</returns>
    IPasswordHasher? GetByAlgorithm(string algorithm, int version);
}

/// <summary>
/// 密码哈希器工厂默认实现。
/// 在启动时注册所有可用的 Hasher，并指定默认 Hasher。
/// </summary>
public sealed class PasswordHasherFactory : IPasswordHasherFactory
{
    private readonly IReadOnlyDictionary<string, IPasswordHasher> _hashers;
    private readonly IPasswordHasher _default;

    public PasswordHasherFactory(IEnumerable<IPasswordHasher> hashers, IPasswordHasher defaultHasher)
    {
        _hashers = hashers.ToDictionary(h => h.Algorithm, StringComparer.OrdinalIgnoreCase);
        _default = defaultHasher;
    }

    public IPasswordHasher GetDefault() => _default;

    public IPasswordHasher? GetByAlgorithm(string algorithm, int version)
    {
        if (_hashers.TryGetValue(algorithm, out var hasher))
            return hasher;
        return null;
    }
}
