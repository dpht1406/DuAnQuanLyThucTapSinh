using System.Text;

namespace StudentInternshipMgmt.Application.Common;

public static class PasswordPolicy
{
    public static bool TryValidate(string? value, out string? error)
    {
        if (value is null || value.Length == 0 || value.EnumerateRunes().All(IsWhitespace))
        {
            error = "Mật khẩu không được để trống hoặc chỉ chứa khoảng trắng.";
            return false;
        }

        if (value.Length < 8)
        {
            error = "Mật khẩu phải có ít nhất 8 ký tự, gồm cả chữ và số.";
            return false;
        }

        if (value.Length > 128)
        {
            error = "Mật khẩu không được vượt quá 128 ký tự.";
            return false;
        }

        var characters = value.EnumerateRunes();
        if (characters.Any(Rune.IsControl))
        {
            error = "Mật khẩu không được chứa ký tự điều khiển.";
            return false;
        }

        if (!value.EnumerateRunes().Any(Rune.IsLetter) || !value.EnumerateRunes().Any(Rune.IsDigit))
        {
            error = "Mật khẩu phải có ít nhất một chữ cái và một chữ số.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsWhitespace(Rune value) =>
        (Rune.IsWhiteSpace(value) && value.Value != 0x85) || value.Value == 0xFEFF;
}
