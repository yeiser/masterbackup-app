using System.Security.Cryptography;
using System.Text;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration configuration)
    {
        var encryptionKey = configuration["Encryption:Key"] 
            ?? throw new InvalidOperationException("Encryption key not configured");
        
        // Generar key de 32 bytes (256 bits) para AES
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
        _iv = new byte[16]; // IV de 16 bytes para AES
        Array.Copy(_key, _iv, 16);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return Convert.ToBase64String(encryptedBytes);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt data. The encryption key may have changed.", ex);
        }
    }
}
