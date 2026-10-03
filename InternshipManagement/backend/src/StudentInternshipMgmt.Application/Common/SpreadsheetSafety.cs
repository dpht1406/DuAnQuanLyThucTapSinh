namespace StudentInternshipMgmt.Application.Common;

public static class SpreadsheetSafety
{
    public static string EscapeFormula(string? value)
    {
        value ??= string.Empty;
        return value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? $"'{value}"
            : value;
    }

    public static string EscapeCsvField(string? value)
    {
        var safeValue = EscapeFormula(value);
        return safeValue.Contains(',') || safeValue.Contains('"') ||
               safeValue.Contains('\n') || safeValue.Contains('\r')
            ? $"\"{safeValue.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : safeValue;
    }
}