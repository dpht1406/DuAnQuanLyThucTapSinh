using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Domain.Entities;

namespace StudentInternshipMgmt.Infrastructure.Services;

public sealed class PlacementRequestEmailNotifier
{
    private const string FrontendHomePath = "/";

    private readonly IEmailSender _emailSender;
    private readonly ILogger<PlacementRequestEmailNotifier> _logger;
    private readonly string _frontendUrl;

    public PlacementRequestEmailNotifier(
        IEmailSender emailSender,
        ILogger<PlacementRequestEmailNotifier> logger,
        IConfiguration? configuration = null)
    {
        _emailSender = emailSender;
        _logger = logger;
        _frontendUrl = (configuration?["App:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
    }

    public Task NotifyApplicationSubmittedAsync(PlacementRequest request, Company company) =>
        NotifySafelyAsync(request.Id, () =>
        {
            if (string.IsNullOrWhiteSpace(company.ContactEmail))
                return Task.CompletedTask;

            var subject = $"Đơn ứng tuyển mới - {company.Name}";
            var details = new[]
            {
                $"Ứng viên: {request.ApplicantFullName}",
                $"Email: {request.ApplicantEmail}",
                $"Vị trí: {request.JobPosition?.Title ?? "Chưa chọn vị trí"}"
            };
            return SendAsync(
                company.ContactEmail,
                company.ContactPerson,
                subject,
                "Bạn vừa nhận được đơn ứng tuyển mới.",
                details);
        });

    public Task NotifyInterviewInvitedAsync(PlacementRequest request) =>
        NotifySafelyAsync(request.Id, () =>
        {
            if (string.IsNullOrWhiteSpace(request.ApplicantEmail))
                return Task.CompletedTask;

            var details = new List<string>
            {
                $"Doanh nghiệp: {request.Company.Name}",
                $"Thời gian: {FormatInterviewAt(request.InterviewAt)}",
                $"Địa điểm: {request.InterviewLocation ?? "Chưa có thông tin"}"
            };
            if (!string.IsNullOrWhiteSpace(request.InterviewNote))
                details.Add($"Ghi chú: {request.InterviewNote}");

            return SendAsync(
                request.ApplicantEmail,
                request.ApplicantFullName,
                $"Thư mời phỏng vấn - {request.Company.Name}",
                "Doanh nghiệp đã mời bạn tham gia phỏng vấn.",
                details);
        });

    public Task NotifyResultAsync(PlacementRequest request, bool passed, string? note = null) =>
        NotifySafelyAsync(request.Id, () =>
        {
            if (string.IsNullOrWhiteSpace(request.ApplicantEmail))
                return Task.CompletedTask;

            var result = passed ? "Đạt" : "Không đạt";
            var details = new List<string>
            {
                $"Doanh nghiệp: {request.Company.Name}",
                $"Kết quả: {result}"
            };
            if (!string.IsNullOrWhiteSpace(note))
                details.Add($"Ghi chú: {note}");

            return SendAsync(
                request.ApplicantEmail,
                request.ApplicantFullName,
                $"Kết quả ứng tuyển: {result} - {request.Company.Name}",
                $"Kết quả xử lý đơn ứng tuyển của bạn: {result}.",
                details);
        });

    public Task NotifyExpiredAsync(PlacementRequest request) =>
        NotifySafelyAsync(request.Id, () =>
        {
            if (string.IsNullOrWhiteSpace(request.ApplicantEmail))
                return Task.CompletedTask;

            return SendAsync(
                request.ApplicantEmail,
                request.ApplicantFullName,
                $"Đơn ứng tuyển đã hết hạn - {request.Company.Name}",
                "Đơn ứng tuyển của bạn đã hết hạn do doanh nghiệp chưa phản hồi trước hạn.",
                [$"Doanh nghiệp: {request.Company.Name}"]);
        });

    private async Task NotifySafelyAsync(int requestId, Func<Task> notification)
    {
        try
        {
            await notification();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Email notification failed for placement request {RequestId}",
                requestId);
        }
    }

    private async Task SendAsync(
        string address,
        string? recipientName,
        string subject,
        string introduction,
        IReadOnlyCollection<string> details)
    {
        var link = _frontendUrl + FrontendHomePath;
        var textBody = $"{introduction}{Environment.NewLine}{Environment.NewLine}"
            + string.Join(Environment.NewLine, details)
            + $"{Environment.NewLine}{Environment.NewLine}Xem thông tin: {link}";
        var htmlDetails = string.Join(
            string.Empty,
            details.Select(detail => $"<li>{WebUtility.HtmlEncode(detail)}</li>"));
        var htmlBody = $"""
            <div>
              <p>{WebUtility.HtmlEncode(introduction)}</p>
              <ul>{htmlDetails}</ul>
              <p><a href="{WebUtility.HtmlEncode(link)}">Mở ứng dụng</a></p>
            </div>
            """;

        try
        {
            var sent = await _emailSender.SendAsync(
                new EmailMessageDto(address, recipientName, subject, textBody, htmlBody),
                CancellationToken.None);
            if (!sent)
                _logger.LogWarning("Email notification could not be delivered");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Email notification failed");
        }
    }

    private static string FormatInterviewAt(DateTime? interviewAt) =>
        interviewAt.HasValue
            ? interviewAt.Value.ToString("dd/MM/yyyy HH:mm 'UTC'")
            : "Chưa có thông tin";
}
