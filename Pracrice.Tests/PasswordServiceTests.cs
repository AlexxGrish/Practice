using System.Security.Cryptography;
using System.Text;
using Pracrice.Services;

namespace Pracrice.Tests;

public sealed class PasswordServiceTests
{
    [Fact]
    public void Hash_ReturnsBcryptHash()
    {
        string hash = PasswordService.Hash("secret123");

        Assert.StartsWith("$2", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify("secret123", hash));
    }

    [Fact]
    public void Verify_AcceptsCorrectPassword()
    {
        string hash = PasswordService.Hash("secret123");

        Assert.True(PasswordService.Verify("secret123", hash));
    }

    [Fact]
    public void Verify_RejectsWrongPassword()
    {
        string hash = PasswordService.Hash("secret123");

        Assert.False(PasswordService.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForMalformedHash()
    {
        Assert.False(PasswordService.Verify("secret123", "not-a-hash"));
    }

    [Fact]
    public void Hash_RejectsEmptyPassword()
    {
        Assert.Throws<ArgumentException>(() => PasswordService.Hash(" "));
    }

    [Fact]
    public void Verify_SupportsLegacySha256()
    {
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("legacy")));

        Assert.True(PasswordService.Verify("legacy", hash));
        Assert.False(PasswordService.Verify("legacy2", hash));
        Assert.True(PasswordService.NeedsRehash(hash));
    }

    [Fact]
    public void BcryptHash_DoesNotNeedRehash()
    {
        string hash = PasswordService.Hash("secret123");

        Assert.False(PasswordService.NeedsRehash(hash));
    }
}
