namespace PeruShopHub.Core.Interfaces;

/// <summary>
/// Encrypts and decrypts marketplace OAuth tokens at rest.
/// </summary>
public interface ITokenEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
