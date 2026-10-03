using System.ComponentModel.DataAnnotations;

namespace StudentInternshipMgmt.Application.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SafeStringAttribute : ValidationAttribute
{
    public SafeStringAttribute(int maxLength, bool allowLineBreaks = false, bool trimBeforeLength = false)
    {
        MaxLength = maxLength;
        AllowLineBreaks = allowLineBreaks;
        TrimBeforeLength = trimBeforeLength;
        ErrorMessage = $"Giá trị không được vượt quá {maxLength} ký tự hoặc chứa ký tự điều khiển không hợp lệ.";
    }

    public int MaxLength { get; }
    public bool AllowLineBreaks { get; }
    public bool TrimBeforeLength { get; }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is not string text || (TrimBeforeLength ? text.Trim().Length : text.Length) > MaxLength)
            return false;

        return !text.Any(character => char.IsControl(character) &&
                          !(AllowLineBreaks && character is ('\r' or '\n')));
    }
}