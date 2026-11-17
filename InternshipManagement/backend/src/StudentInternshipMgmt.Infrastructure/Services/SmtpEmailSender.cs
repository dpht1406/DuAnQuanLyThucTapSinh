using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Infrastructure.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(10);

    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessageDto message, CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(SendTimeout);

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(
                CleanHeader(_configuration["Smtp:FromName"] ?? string.Empty),
                _configuration["Smtp:FromAddress"] ?? string.Empty));
            mimeMessage.To.Add(new MailboxAddress(
                CleanHeader(message.ToName),
                CleanHeader(message.ToAddress)));
            mimeMessage.Subject = CleanHeader(message.Subject);
            mimeMessage.Body = new BodyBuilder
            {
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody
            }.ToMessageBody();

            using var client = new SmtpClient
            {
                Timeout = (int)SendTimeout.TotalMilliseconds
            };

            var host = _configuration["Smtp:Host"];
            var port = _configuration.GetValue<int?>("Smtp:Port");
            if (string.IsNullOrWhiteSpace(host) || !port.HasValue || port.Value is < 1 or > 65535)
                throw new InvalidOperationException("SMTP host and a valid port are required when SMTP is enabled.");

            var secureSocketOptions = _configuration.GetValue<bool>("Smtp:UseStartTls")
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;
            await client.ConnectAsync(host, port.Value, secureSocketOptions, timeout.Token);

            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            if (!string.IsNullOrWhiteSpace(username))
                await client.AuthenticateAsync(username, password ?? string.Empty, timeout.Token);

            await client.SendAsync(mimeMessage, timeout.Token);
            await client.DisconnectAsync(true, timeout.Token);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Email delivery failed");
            return false;
        }
    }

    private static string CleanHeader(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}
