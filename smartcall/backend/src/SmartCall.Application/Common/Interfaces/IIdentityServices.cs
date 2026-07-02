namespace SmartCall.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, bool isSuperAdmin);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsSuperAdmin { get; }
}

/// <summary>AES encryption for sensitive settings (OpenAI API key, SMTP password).</summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
