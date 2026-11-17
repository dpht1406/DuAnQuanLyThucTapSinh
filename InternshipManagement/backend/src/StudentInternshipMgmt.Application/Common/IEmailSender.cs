namespace StudentInternshipMgmt.Application.Common;

public interface IEmailSender
{
    Task<bool> SendAsync(EmailMessageDto message, CancellationToken ct);
}
