namespace CryptoPlatform.Application.Crypto;

// ─── SM4 ───

/// <summary>SM4 加密请求</summary>
public sealed record Sm4EncryptRequest(
    string KeyId, int? KeyVersion, string Plaintext,
    string Mode, string Encoding, string? Nonce, string? Aad);

/// <summary>SM4 加密响应</summary>
public sealed record Sm4EncryptResponse(
    string KeyId, int KeyVersion, string Algorithm,
    string Ciphertext, string Nonce, string Tag, string Encoding);

/// <summary>SM4 解密请求</summary>
public sealed record Sm4DecryptRequest(
    string KeyId, int? KeyVersion, string Ciphertext,
    string Mode, string Encoding, string Nonce, string Tag, string? Aad);

/// <summary>SM4 解密响应</summary>
public sealed record Sm4DecryptResponse(
    string KeyId, int KeyVersion, string Plaintext, string Encoding);

// ─── SM2 ───

/// <summary>SM2 加密请求</summary>
public sealed record Sm2EncryptRequest(string KeyId, int? KeyVersion, string Data, string Encoding);
/// <summary>SM2 加密响应</summary>
public sealed record Sm2EncryptResponse(string KeyId, int KeyVersion, string Ciphertext, string Encoding);
/// <summary>SM2 解密请求</summary>
public sealed record Sm2DecryptRequest(string KeyId, int? KeyVersion, string Ciphertext, string Encoding);
/// <summary>SM2 解密响应</summary>
public sealed record Sm2DecryptResponse(string KeyId, int KeyVersion, string Data, string Encoding);
/// <summary>SM2 签名请求</summary>
public sealed record Sm2SignRequest(string KeyId, int? KeyVersion, string Data, string Encoding);
/// <summary>SM2 签名响应</summary>
public sealed record Sm2SignResponse(string KeyId, int KeyVersion, string Signature, string Encoding);
/// <summary>SM2 验签请求</summary>
public sealed record Sm2VerifyRequest(string KeyId, int? KeyVersion, string Data, string Signature, string Encoding);
/// <summary>SM2 验签响应</summary>
public sealed record Sm2VerifyResponse(string KeyId, int KeyVersion, bool Valid);

// ─── SM3 ───

/// <summary>SM3 杂凑请求</summary>
public sealed record Sm3HashRequest(string Data, string InputEncoding, string OutputEncoding);
/// <summary>SM3 杂凑响应</summary>
public sealed record Sm3HashResponse(string Hash, string Encoding);

// ─── HMAC-SM3 ───

/// <summary>HMAC-SM3 生成请求</summary>
public sealed record HmacGenerateRequest(string KeyId, int? KeyVersion, string Data, string Encoding);
/// <summary>HMAC-SM3 生成响应</summary>
public sealed record HmacGenerateResponse(string KeyId, int KeyVersion, string Hmac, string Encoding);
/// <summary>HMAC-SM3 验证请求</summary>
public sealed record HmacVerifyRequest(string KeyId, int? KeyVersion, string Data, string Hmac, string Encoding);
/// <summary>HMAC-SM3 验证响应</summary>
public sealed record HmacVerifyResponse(string KeyId, int KeyVersion, bool Valid);

// ─── Random ───

/// <summary>随机数生成请求</summary>
public sealed record RandomRequest(int Length, string Encoding);
/// <summary>随机数生成响应</summary>
public sealed record RandomResponse(string Data, string Encoding);
