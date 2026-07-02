using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using SmartCall.Application.Common.Interfaces;

namespace SmartCall.Infrastructure.Services;

/// <summary>
/// AES-256-GCM encryption for sensitive settings at rest. The key is derived
/// from the SMARTCALL_DATA_KEY environment variable / configuration value.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var secret = configuration["SMARTCALL_DATA_KEY"]
            ?? configuration["DataProtection:Key"]
            ?? throw new InvalidOperationException("SMARTCALL_DATA_KEY is not configured.");
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);

        var payload = new byte[nonce.Length + tag.Length + cipher.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, nonce.Length);
        cipher.CopyTo(payload, nonce.Length + tag.Length);
        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        var payload = Convert.FromBase64String(cipherText);
        var nonce = payload.AsSpan(0, 12).ToArray();
        var tag = payload.AsSpan(12, 16).ToArray();
        var cipher = payload.AsSpan(28).ToArray();
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
