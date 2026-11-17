namespace StudentInternshipMgmt.Application.Common;

public sealed record EmailMessageDto
{
    public EmailMessageDto(
        string toAddress,
        string? toName,
        string subject,
        string textBody,
        string htmlBody)
    {
        ToAddress = RemoveLineBreaks(toAddress);
        ToName = RemoveLineBreaks(toName ?? string.Empty);
        Subject = RemoveLineBreaks(subject);
        TextBody = textBody;
        HtmlBody = htmlBody;
    }

    public string ToAddress { get; }
    public string ToName { get; }
    public string Subject { get; }
    public string TextBody { get; }
    public string HtmlBody { get; }

    private static string RemoveLineBreaks(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}
