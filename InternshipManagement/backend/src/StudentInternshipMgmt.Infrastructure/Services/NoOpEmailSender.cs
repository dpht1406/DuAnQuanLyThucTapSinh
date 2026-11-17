using Microsoft.Extensions.Logging;
using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Infrastructure.Services;

public sealed class NoOpEmailSender : IEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;

    public NoOpEmailSender(ILogger<NoOpEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(EmailMessageDto message, CancellationToken ct)
    {
        _logger.LogInformation("Email notifications are disabled; message not sent");
        return Task.FromResult(true);
    }
}
