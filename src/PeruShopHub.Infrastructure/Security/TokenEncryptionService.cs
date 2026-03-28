using Microsoft.AspNetCore.DataProtection;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Security;

public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly IDataProtector _protector;

    public TokenEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("MarketplaceTokens");
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrEmpty(cipherText);
        return _protector.Unprotect(cipherText);
    }
}
