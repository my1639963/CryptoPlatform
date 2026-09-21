namespace CryptoPlatform.Application.Crypto;

/// <summary>
/// 密码运算服务接口。
/// 提供 SM2/SM4/SM3/HMAC-SM3 全算法密码运算能力。
/// 所有操作均需通过授权校验和状态校验。
/// </summary>
public interface ICryptoService
{
    // SM4
    Task<Sm4EncryptResponse> Sm4EncryptAsync(Sm4EncryptRequest req, CancellationToken ct);
    Task<Sm4DecryptResponse> Sm4DecryptAsync(Sm4DecryptRequest req, CancellationToken ct);

    // SM2
    Task<Sm2EncryptResponse> Sm2EncryptAsync(Sm2EncryptRequest req, CancellationToken ct);
    Task<Sm2DecryptResponse> Sm2DecryptAsync(Sm2DecryptRequest req, CancellationToken ct);
    Task<Sm2SignResponse> Sm2SignAsync(Sm2SignRequest req, CancellationToken ct);
    Task<Sm2VerifyResponse> Sm2VerifyAsync(Sm2VerifyRequest req, CancellationToken ct);

    // SM3
    Task<Sm3HashResponse> Sm3HashAsync(Sm3HashRequest req, CancellationToken ct);

    // HMAC-SM3
    Task<HmacGenerateResponse> HmacGenerateAsync(HmacGenerateRequest req, CancellationToken ct);
    Task<HmacVerifyResponse> HmacVerifyAsync(HmacVerifyRequest req, CancellationToken ct);

    // Random
    Task<RandomResponse> GenerateRandomAsync(RandomRequest req, CancellationToken ct);
}
