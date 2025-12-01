namespace MasterBackup_API.Application.Common.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
}
