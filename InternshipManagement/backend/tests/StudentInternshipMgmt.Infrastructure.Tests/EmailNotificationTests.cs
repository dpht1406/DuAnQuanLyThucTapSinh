using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class EmailNotificationTests
{
    [Fact]
    public async Task Request_submission_and_interview_invitation_send_to_expected_recipients()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        fixture.Company.ContactEmail = "recruiting@example.test";
        var sender = new FakeEmailSender();
        var service = CreatePlacementRequestService(db, sender);
        var submitted = await service.CreateRequestAsync(fixture.Student.Id, new CreatePlacementRequestDto
        {
            CompanyId = fixture.Company.Id,
            JobPositionId = fixture.Position.Id,
            ApplicantFullName = "<script>alert(1)</script>",
            ApplicantEmail = "applicant@example.test",
            ApplicantPhone = "0900000000",
            ApplicantSchool = "Trường",
            ApplicantMajor = "Ngành",
            CvUrl = "https://example.test/cv"
        });

        submitted.Success.Should().BeTrue();
        sender.Messages.Should().ContainSingle();
        sender.Messages[0].ToAddress.Should().Be("recruiting@example.test");
        sender.Messages[0].HtmlBody.Should().Contain("&lt;script&gt;");
        sender.Messages[0].HtmlBody.Should().NotContain("<script>");

        var invited = await service.InviteCompanyRequestToInterviewAsync(
            fixture.Company.Id,
            submitted.Data!.Id,
            new InviteInterviewDto
            {
                InterviewAt = DateTime.UtcNow.AddDays(1),
                InterviewLocation = "Phòng A",
                InterviewNote = "Mang CV"
            },
            companyUserId: 99);

        invited.Success.Should().BeTrue();
        sender.Messages.Should().HaveCount(2);
        sender.Messages[1].ToAddress.Should().Be("applicant@example.test");
        sender.Messages[1].TextBody.Should().Contain("Phòng A");
        sender.Messages[1].TextBody.Should().Contain("Mang CV");
        sender.Messages[1].TextBody.Should().Contain("UTC");
    }

    [Fact]
    public async Task Company_result_sends_once_and_delivery_failure_does_not_undo_result()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(fixture, RequestStatus.Pending);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender { ThrowOnSend = true };
        var service = CreatePlacementRequestService(db, sender);

        var result = await service.RecordCompanyResultAsync(
            fixture.Company.Id,
            request.Id,
            new CompanyPlacementResultDto { IsPassed = false, Note = "Không đạt" },
            companyUserId: 99);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        request.Status.Should().Be(RequestStatus.Rejected);
        sender.Messages.Should().ContainSingle();
        sender.Messages[0].ToAddress.Should().Be("applicant@example.test");
    }

    [Fact]
    public async Task Admin_failed_result_sends_to_applicant_after_successful_commit()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(fixture, RequestStatus.InterviewInvited);
        request.StudentReportedInterviewAt = DateTime.UtcNow.AddHours(-1);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender();
        var service = CreatePlacementRequestService(db, sender);

        var result = await service.AdminConfirmResultAsync(request.Id, 55, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Failed,
            ConfirmationSource = "Đã xác minh",
            Note = "Không đạt"
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        request.Status.Should().Be(RequestStatus.Rejected);
        sender.Messages.Should().ContainSingle();
        sender.Messages[0].ToAddress.Should().Be("applicant@example.test");
        sender.Messages[0].TextBody.Should().Contain("Không đạt");
    }

    [Fact]
    public async Task Company_passed_result_sends_once_to_applicant()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(fixture, RequestStatus.Pending);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender();
        var service = CreatePlacementRequestService(db, sender);

        var result = await service.RecordCompanyResultAsync(
            fixture.Company.Id,
            request.Id,
            new CompanyPlacementResultDto { IsPassed = true, Note = "Đạt" },
            companyUserId: 99);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        request.Status.Should().Be(RequestStatus.Accepted);
        sender.Messages.Should().ContainSingle();
        sender.Messages[0].ToAddress.Should().Be("applicant@example.test");
        sender.Messages[0].TextBody.Should().Contain("Đạt");
    }

    [Fact]
    public async Task Expired_request_notifies_once_and_delivery_failure_keeps_expiry()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(fixture, RequestStatus.Pending);
        request.DueAt = DateTime.UtcNow.AddDays(-1);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender { ThrowOnSend = true };
        var notifier = new PlacementRequestEmailNotifier(
            sender,
            NullLogger<PlacementRequestEmailNotifier>.Instance);
        var service = new PlacementRequestExpiryService(
            db,
            NullLogger<PlacementRequestExpiryService>.Instance,
            notifier);

        var expiredCount = await service.ExpireOverdueAsync(DateTime.UtcNow, CancellationToken.None);

        expiredCount.Should().Be(1);
        request.Status.Should().Be(RequestStatus.Expired);
        sender.Messages.Should().ContainSingle();
        sender.Messages[0].ToAddress.Should().Be("applicant@example.test");
    }

    [Fact]
    public async Task Message_headers_are_cleaned_and_no_op_sender_does_not_throw()
    {
        var message = new EmailMessageDto(
            "recipient@example.test",
            "Tên\r\nNgười nhận",
            "Tiêu đề\r\nInjected: yes",
            "Nội dung",
            "<p>Nội dung</p>");

        message.ToName.Should().Be("TênNgười nhận");
        message.Subject.Should().Be("Tiêu đềInjected: yes");
        await new NoOpEmailSender(NullLogger<NoOpEmailSender>.Instance)
            .SendAsync(message, CancellationToken.None);
    }

    private static PlacementRequestService CreatePlacementRequestService(AppDbContext db, IEmailSender sender)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:FrontendBaseUrl"] = "https://internship.example.test"
            })
            .Build();
        var notifier = new PlacementRequestEmailNotifier(
            sender,
            NullLogger<PlacementRequestEmailNotifier>.Instance,
            configuration);
        var acceptanceService = new PlacementAcceptanceService(
            db,
            NullLogger<PlacementAcceptanceService>.Instance,
            notifier);
        return new PlacementRequestService(
            db,
            NullLogger<PlacementRequestService>.Instance,
            configuration,
            acceptanceService,
            notifier);
    }

    private static async Task<TestFixture> SeedAsync(AppDbContext db)
    {
        var company = new Company
        {
            Id = 1,
            Name = "Doanh nghiệp",
            Address = "Địa chỉ",
            Industry = "Công nghệ",
            ContactPerson = "Người liên hệ",
            ContactEmail = "company@example.test"
        };
        var position = new JobPosition
        {
            Id = 1,
            Company = company,
            CompanyId = company.Id,
            Title = "Thực tập sinh",
            Department = "Công nghệ",
            Quantity = 1,
            Description = "Mô tả",
            IsOpen = true,
            ApprovalStatus = JobPositionApprovalStatus.Approved
        };
        var student = new Student
        {
            Id = 1,
            StudentCode = "SV0001",
            FullName = "Sinh viên",
            Major = "Công nghệ",
            ClassName = "SE01",
            Email = "profile@example.test",
            PhoneNumber = "0911111111",
            Status = StudentStatus.NoCompany,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.AddRange(company, position, student);
        await db.SaveChangesAsync();
        return new TestFixture(company, position, student);
    }

    private static PlacementRequest CreateRequest(TestFixture fixture, RequestStatus status) => new()
    {
        StudentId = fixture.Student.Id,
        Student = fixture.Student,
        CompanyId = fixture.Company.Id,
        Company = fixture.Company,
        JobPositionId = fixture.Position.Id,
        JobPosition = fixture.Position,
        ApplicantFullName = "Ứng viên",
        ApplicantEmail = "applicant@example.test",
        Status = status,
        CreatedAt = DateTime.UtcNow,
        DueAt = DateTime.UtcNow.AddDays(7)
    };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private sealed record TestFixture(Company Company, JobPosition Position, Student Student);

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessageDto> Messages { get; } = [];
        public bool ThrowOnSend { get; init; }

        public Task<bool> SendAsync(EmailMessageDto message, CancellationToken ct)
        {
            Messages.Add(message);
            if (ThrowOnSend)
                throw new InvalidOperationException("SMTP unavailable");
            return Task.FromResult(true);
        }
    }
}
