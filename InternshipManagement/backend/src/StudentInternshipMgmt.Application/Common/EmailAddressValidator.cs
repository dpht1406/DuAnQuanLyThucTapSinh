using System.Text.RegularExpressions;

namespace StudentInternshipMgmt.Application.Common;

public static class EmailAddressValidator
{
    public static readonly string[] PopularDomains =
    {
        "gmail.com", "outlook.com", "hotmail.com", "yahoo.com", "icloud.com", "live.com"
    };

    private static readonly HashSet<string> BlockedTypos = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.co", "gmail.con", "gmail.cm", "gmail.comm", "gmial.com", "gmai.com",
        "gmal.com", "gamil.com", "gnail.com", "hotmal.com", "hotmail.co", "outlok.com",
        "outlook.co", "yaho.com", "yahoo.co"
    };

    private static readonly Regex LocalPartPattern = new(
        @"^[A-Za-z0-9._%+-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex DomainLabelPattern = new(
        @"^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TopLevelDomainPattern = new(
        @"^[A-Za-z]{2,}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValid(string? value) => TryValidate(value, out _);

    public static string GetErrorMessage(string? value)
    {
        TryValidate(value, out var errorMessage);
        return errorMessage ?? "Email không đúng định dạng.";
    }

    public static bool TryValidate(string? value, out string? errorMessage)
    {
        errorMessage = null;
        if (value is not null && value.Any(char.IsControl))
        {
            errorMessage = "Email không được chứa ký tự điều khiển.";
            return false;
        }

        var email = value?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > 254 ||
            email.Count(character => character == '@') != 1 ||
            email.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
        {
            errorMessage = "Email không đúng định dạng hoặc vượt quá 254 ký tự.";
            return false;
        }

        var separator = email.IndexOf('@');
        var localPart = email[..separator];
        var domain = email[(separator + 1)..];
        if (localPart.Length is 0 or > 64 ||
            !LocalPartPattern.IsMatch(localPart) ||
            localPart.StartsWith('.') || localPart.EndsWith('.') || localPart.Contains(".."))
        {
            errorMessage = "Phần trước @ của email không hợp lệ.";
            return false;
        }

        var normalizedDomain = domain.ToLowerInvariant();
        if (PopularDomains.Contains(normalizedDomain, StringComparer.OrdinalIgnoreCase))
            return true;

        var suggestionDomain = PopularDomains
            .Select(candidate => (Domain: candidate, Distance: LevenshteinDistance(normalizedDomain, candidate)))
            .Where(candidate => candidate.Distance <= 2)
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => Array.IndexOf(PopularDomains, candidate.Domain))
            .Select(candidate => candidate.Domain)
            .FirstOrDefault();

        if (BlockedTypos.Contains(normalizedDomain) || suggestionDomain is not null)
        {
            suggestionDomain ??= PopularDomains
                .OrderBy(candidate => LevenshteinDistance(normalizedDomain, candidate))
                .First();
            errorMessage = $"Email có vẻ gõ sai. Ý bạn là {localPart}@{suggestionDomain}?";
            return false;
        }

        var labels = domain.Split('.');
        if (labels.Length < 2 || labels.Any(label =>
                label.Length is 0 or > 63 || !DomainLabelPattern.IsMatch(label)) ||
            !TopLevelDomainPattern.IsMatch(labels[^1]))
        {
            errorMessage = "Tên miền email không đúng định dạng.";
            return false;
        }

        return true;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        var current = new int[right.Length + 1];

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var substitutionCost = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}