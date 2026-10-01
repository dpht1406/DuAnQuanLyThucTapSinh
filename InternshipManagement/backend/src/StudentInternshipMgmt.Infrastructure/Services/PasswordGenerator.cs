using System.Security.Cryptography;

namespace StudentInternshipMgmt.Infrastructure.Services;

public static class PasswordGenerator
{
    public static string Generate(int length = 10)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = upper + lower + digits;

        Span<byte> buffer = stackalloc byte[length];
        RandomNumberGenerator.Fill(buffer);

        var chars = new char[length];
        chars[0] = upper[buffer[0] % upper.Length];
        chars[1] = lower[buffer[1] % lower.Length];
        chars[2] = digits[buffer[2] % digits.Length];
        for (int i = 3; i < length; i++)
            chars[i] = all[buffer[i] % all.Length];

        for (int i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}