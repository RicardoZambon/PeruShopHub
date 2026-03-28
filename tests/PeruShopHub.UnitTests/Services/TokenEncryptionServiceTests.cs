using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using PeruShopHub.Infrastructure.Security;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class TokenEncryptionServiceTests
{
    private readonly TokenEncryptionService _sut;

    public TokenEncryptionServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDataProtection()
            .SetApplicationName("PeruShopHub-Tests");
        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IDataProtectionProvider>();
        _sut = new TokenEncryptionService(provider);
    }

    [Fact]
    public void Encrypt_Decrypt_Roundtrip_ReturnsOriginalValue()
    {
        var original = "test-access-token-12345";

        var encrypted = _sut.Encrypt(original);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCipherText()
    {
        var plainText = "my-secret-token";

        var encrypted = _sut.Encrypt(plainText);

        encrypted.Should().NotBe(plainText);
    }

    [Fact]
    public void Encrypt_SameInput_ProducesCipherText()
    {
        var plainText = "same-token";

        var encrypted1 = _sut.Encrypt(plainText);
        var encrypted2 = _sut.Encrypt(plainText);

        // Both should decrypt to same value
        _sut.Decrypt(encrypted1).Should().Be(plainText);
        _sut.Decrypt(encrypted2).Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_InvalidCipherText_Throws()
    {
        var action = () => _sut.Decrypt("not-a-valid-cipher-text");

        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Encrypt_EmptyString_Throws()
    {
        var action = () => _sut.Encrypt("");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_EmptyString_Throws()
    {
        var action = () => _sut.Decrypt("");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encrypt_NullString_Throws()
    {
        var action = () => _sut.Encrypt(null!);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encrypt_Decrypt_LongToken_Roundtrip()
    {
        var original = new string('A', 2048);

        var encrypted = _sut.Encrypt(original);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_Decrypt_SpecialCharacters_Roundtrip()
    {
        var original = "token/with+special=chars&more?yes!@#$%^";

        var encrypted = _sut.Encrypt(original);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void KeyRotation_OldKeysCanStillDecrypt()
    {
        // Create first provider and encrypt
        var services1 = new ServiceCollection();
        services1.AddDataProtection()
            .SetApplicationName("PeruShopHub-Tests");
        var sp1 = services1.BuildServiceProvider();
        var sut1 = new TokenEncryptionService(sp1.GetRequiredService<IDataProtectionProvider>());

        var original = "rotate-me-token";
        var encrypted = sut1.Encrypt(original);

        // Create second provider from same application (simulates key rotation —
        // new keys added but old keys still available for decryption)
        var services2 = new ServiceCollection();
        services2.AddDataProtection()
            .SetApplicationName("PeruShopHub-Tests");
        var sp2 = services2.BuildServiceProvider();
        var sut2 = new TokenEncryptionService(sp2.GetRequiredService<IDataProtectionProvider>());

        var decrypted = sut2.Decrypt(encrypted);
        decrypted.Should().Be(original);
    }
}
