using System.Security.Cryptography;
using System.Text;

namespace Pracrice.Services;

public static class PasswordService
{
    private const int WorkFactor = 11;

    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Пароль не может быть пустым.", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public static bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            if (passwordHash.StartsWith("$2", StringComparison.Ordinal))
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }

            if (passwordHash.Length == 64 && IsHex(passwordHash))
            {
                byte[] expected = Convert.FromHexString(passwordHash);
                byte[] actual = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }

    public static bool NeedsRehash(string passwordHash) =>
        !passwordHash.StartsWith("$2", StringComparison.Ordinal);

    private static bool IsHex(string value) =>
        value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
}
