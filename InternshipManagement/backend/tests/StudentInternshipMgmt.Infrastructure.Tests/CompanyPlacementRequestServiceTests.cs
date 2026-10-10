using System.Reflection;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Api.Controllers;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class CompanyPlacementRequestServiceTests
{
    [Fact]
    public async Task Company_request_list_is_scoped_filtered_and_excludes_deleted_students()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var secondOwnPosition = CreatePosition(12, fixture.CompanyA);
        var deletedStudent = CreateStudent(3);
        deletedStudent.IsDeleted = true;
        db.JobPositions.Add(secondOwnPosition);
        db.Students.Add(deletedStudent);
        await db.SaveChangesAsync();
        db.PlacementRequests.AddRange(
            CreateRequest(101, fixture.CompanyA, fixture.StudentA, fixture.PositionA, RequestStatus.Pending, DateTime.UtcNow),
            CreateRequest(102, fixture.CompanyA, fixture.StudentA, fixture.PositionA, RequestStatus.Rejected, DateTime.UtcNow.AddMinutes(-1)),
            CreateRequest(105, fixture.CompanyA, fixture.StudentA, secondOwnPosition, RequestStatus.Pending, DateTime.UtcNow.AddMinutes(-3)),
            CreateRequest(106, fixture.CompanyA, deletedStudent, fixture.PositionA, RequestStatus.Pending, DateTime.UtcNow.AddMinutes(-4)),
            CreateRequest(104, fixture.CompanyB, fixture.StudentB, fixture.PositionB, RequestStatus.Pending, DateTime.UtcNow.AddMinutes(1)));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var all = await service.GetCompanyRequestsAsync(1, new CompanyPlacementRequestFilterDto { PageSize = 20 });
        var filtered = await service.GetCompanyRequestsAsync(1, new CompanyPlacementRequestFilterDto
        {
            Status = RequestStatus.Pending,
            JobPositionId = fixture.PositionA.Id,
            PageSize = 20
        });
        var rejected = await service.GetCompanyRequestsAsync(1, new CompanyPlacementRequestFilterDto
        {
            Status = RequestStatus.Rejected,
            PageSize = 20
        });
        var filteredByPosition = await service.GetCompanyRequestsAsync(1, new CompanyPlacementRequestFilterDto
        {
            JobPositionId = secondOwnPosition.Id,
            PageSize = 20
        });

        all.Items.Select(request => request.Id).Should().Equal(101, 102, 105);
        filtered.Items.Select(request => request.Id).Should().Equal(101);
        rejected.Items.Select(request => request.Id).Should().Equal(102);
        filteredByPosition.Items.Select(request => request.Id).Should().Equal(105);
    }

    [Fact]
    public async Task Company_cannot_read_or_mutate_another_company_request()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var foreignRequest = CreateRequest(
            201, fixture.CompanyB, fixture.StudentB, fixture.PositionB, RequestStatus.Pending, DateTime.UtcNow);
        foreignRequest.DueAt = DateTime.UtcNow.AddDays(5);
        db.PlacementRequests.Add(foreignRequest);
        await db.SaveChangesAsync();
        var originalDueAt = foreignRequest.DueAt;
        var service = CreateService(db);

        var details = await service.GetCompanyRequestByIdAsync(1, foreignRequest.Id);
        var invite = await service.InviteCompanyRequestToInterviewAsync(
            1, foreignRequest.Id, ValidInvite(), companyUserId: 99);
        var reject = await service.RejectCompanyRequestAsync(
            1, foreignRequest.Id, new CompanyRejectPlacementRequestDto { Reason = "Không đạt." }, companyUserId: 99);

        details.Should().BeNull();
        invite.NotFound.Should().BeTrue();
        reject.NotFound.Should().BeTrue();
        var unchanged = await db.PlacementRequests.SingleAsync(request => request.Id == foreignRequest.Id);
        unchanged.Status.Should().Be(RequestStatus.Pending);
        unchanged.InterviewAt.Should().BeNull();
        unchanged.RejectReason.Should().BeNull();
        unchanged.DueAt.Should().Be(originalDueAt);
    }

    [Fact]
    public async Task Invite_interview_updates_only_request_and_stores_interview_time_as_utc()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var dueAt = DateTime.UtcNow.AddDays(7);
        var request = CreateRequest(301, fixture.CompanyA, fixture.StudentA, fixture.PositionA, RequestStatus.Pending, DateTime.UtcNow);
        request.DueAt = dueAt;
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();
        var expectedInterviewAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2), DateTimeKind.Utc);
        var unspecifiedInterviewAt = DateTime.SpecifyKind(expectedInterviewAt, DateTimeKind.Unspecified);
        var studentSnapshot = StudentState(fixture.StudentA);
        var service = CreateService(db);

        var result = await service.InviteCompanyRequestToInterviewAsync(
            1,
            request.Id,
            new InviteInterviewDto
            {
                InterviewAt = unspecifiedInterviewAt,
                InterviewLocation = "  Phòng A\r\n ",
                InterviewNote = "  Vui lòng\r\nchuẩn bị.  "
            },
            companyUserId: 77);

        result.Success.Should().BeTrue();
        request.Status.Should().Be(RequestStatus.InterviewInvited);
        request.InterviewAt.Should().Be(expectedInterviewAt);
        request.InterviewAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        request.InterviewLocation.Should().Be("Phòng A");
        request.InterviewNote.Should().Be("Vui lòng\r\nchuẩn bị.");
        request.ReviewedBy.Should().Be(77);
        request.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.DueAt.Should().Be(dueAt);
        StudentState(fixture.StudentA).Should().BeEquivalentTo(studentSnapshot);
        (await db.StatusHistories.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Invite_interview_rejects_non_pending_request()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(
            302, fixture.CompanyA, fixture.StudentA, fixture.PositionA, RequestStatus.InterviewInvited, DateTime.UtcNow);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var result = await CreateService(db).InviteCompanyRequestToInterviewAsync(
            1, request.Id, ValidInvite(), companyUserId: 77);

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Error.Should().Contain("Chỉ có thể mời phỏng vấn");
        request.Status.Should().Be(RequestStatus.InterviewInvited);
    }

    [Fact]
    public void Interview_invitation_validator_requires_a_future_time_and_location()
    {
        var validator = new InviteInterviewDtoValidator();
        var result = validator.TestValidate(new InviteInterviewDto
        {
            InterviewAt = DateTime.UtcNow.AddMinutes(-1),
            InterviewLocation = " "
        });

        result.ShouldHaveValidationErrorFor(request => request.InterviewAt)
            .WithErrorMessage("Thời gian phỏng vấn phải ở tương lai.");
        result.ShouldHaveValidationErrorFor(request => request.InterviewLocation)
            .WithErrorMessage("Địa điểm phỏng vấn là bắt buộc.");

        validator.TestValidate(new InviteInterviewDto
            {
                InterviewAt = DateTime.UtcNow.AddDays(1),
                InterviewLocation = new string('x', 501)
            })
            .ShouldHaveValidationErrorFor(request => request.InterviewLocation)
            .WithErrorMessage("Địa điểm phỏng vấn không được vượt quá 500 ký tự.");
    }

    [Theory]
    [InlineData(RequestStatus.Pending)]
    [InlineData(RequestStatus.InterviewInvited)]
    public async Task Company_can_reject_pending_or_interview_invited_request_without_changing_student(
        RequestStatus initialStatus)
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(401, fixture.CompanyA, fixture.StudentA, fixture.PositionA, initialStatus, DateTime.UtcNow);
        var dueAt = DateTime.UtcNow.AddDays(4);
        request.DueAt = dueAt;
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();
        var studentSnapshot = StudentState(fixture.StudentA);

        var result = await CreateService(db).RejectCompanyRequestAsync(
            1,
            request.Id,
            new CompanyRejectPlacementRequestDto { Reason = "  Hồ sơ chưa phù hợp.  " },
            companyUserId: 77);

        result.Success.Should().BeTrue();
        request.Status.Should().Be(RequestStatus.Rejected);
        request.RejectReason.Should().Be("Hồ sơ chưa phù hợp.");
        request.ResultActorType.Should().Be(ActorType.Company);
        request.ReviewedBy.Should().Be(77);
        request.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.DueAt.Should().Be(dueAt);
        StudentState(fixture.StudentA).Should().BeEquivalentTo(studentSnapshot);
        (await db.StatusHistories.CountAsync()).Should().Be(0);

        var repeated = await CreateService(db).RejectCompanyRequestAsync(
            1,
            request.Id,
            new CompanyRejectPlacementRequestDto { Reason = "Lý do thay thế." },
            companyUserId: 88);
        repeated.Success.Should().BeFalse();
        repeated.NotFound.Should().BeFalse();
        repeated.Error.Should().Be("Đơn này đã được xử lý.");
        request.RejectReason.Should().Be("Hồ sơ chưa phù hợp.");
    }

    [Fact]
    public void Company_rejection_validator_requires_a_nonblank_reason_within_limit()
    {
        var validator = new CompanyRejectPlacementRequestDtoValidator();

        validator.TestValidate(new CompanyRejectPlacementRequestDto { Reason = " " })
            .ShouldHaveValidationErrorFor(request => request.Reason)
            .WithErrorMessage("Lý do từ chối là bắt buộc.");
        validator.TestValidate(new CompanyRejectPlacementRequestDto { Reason = new string('x', 1001) })
            .ShouldHaveValidationErrorFor(request => request.Reason)
            .WithErrorMessage("Lý do từ chối không được vượt quá 1000 ký tự.");
    }

    [Theory]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Accepted)]
    [InlineData(RequestStatus.Expired)]
    public async Task Company_rejects_repeat_or_finalized_requests(RequestStatus initialStatus)
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(501, fixture.CompanyA, fixture.StudentA, fixture.PositionA, initialStatus, DateTime.UtcNow);
        request.RejectReason = initialStatus == RequestStatus.Rejected ? "Lý do cũ" : null;
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.RejectCompanyRequestAsync(
            1,
            request.Id,
            new CompanyRejectPlacementRequestDto { Reason = "Lý do mới" },
            companyUserId: 77);
        var invite = await service.InviteCompanyRequestToInterviewAsync(
            1, request.Id, ValidInvite(), companyUserId: 77);

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Error.Should().Be("Đơn này đã được xử lý.");
        invite.Success.Should().BeFalse();
        invite.NotFound.Should().BeFalse();
        invite.Error.Should().Be("Đơn này đã được xử lý.");
        request.Status.Should().Be(initialStatus);
        if (initialStatus == RequestStatus.Rejected)
            request.RejectReason.Should().Be("Lý do cũ");
    }

    [Fact]
    public void Company_placement_request_actions_are_company_authorized_and_dtos_exclude_profile_contact_fields()
    {
        var controllerAuthorization = typeof(CompanyMeController).GetCustomAttribute<AuthorizeAttribute>();
        controllerAuthorization.Should().NotBeNull();
        controllerAuthorization!.Roles.Should().Be("Company");

        var actions = new[]
        {
            nameof(CompanyMeController.GetMyPlacementRequests),
            nameof(CompanyMeController.GetMyPlacementRequest),
            nameof(CompanyMeController.InviteToInterview),
            nameof(CompanyMeController.RejectPlacementRequest),
            nameof(CompanyMeController.RecordPlacementResult)
        };
        foreach (var action in actions)
        {
            typeof(CompanyMeController).GetMethod(action).Should().NotBeNull();
        }

        typeof(CompanyPlacementRequestFilterDto).GetProperty("CompanyId").Should().BeNull();
        foreach (var propertyName in new[]
        {
            "StudentCode",
            "StudentReportedInterviewAt",
            "StudentInterviewNote",
            "ConfirmationSource",
            "CompanyId",
            "Email",
            "PhoneNumber"
        })
        {
            typeof(CompanyPlacementRequestDto).GetProperty(propertyName).Should().BeNull();
        }

        var contactProperties = typeof(CompanyPlacementRequestDto).GetProperties()
            .Where(property => property.Name.Contains("Email", StringComparison.Ordinal)
                || property.Name.Contains("Phone", StringComparison.Ordinal))
            .Select(property => property.Name);
        contactProperties.Should().BeEquivalentTo("ApplicantEmail", "ApplicantPhone");
        typeof(CompanyPlacementRequestDto).GetProperty("ReviewedByUsername").Should().BeNull();
        typeof(CompanyPlacementRequestDto).GetProperty("ConfirmationSource").Should().BeNull();
    }

    private static InviteInterviewDto ValidInvite() => new()
    {
        InterviewAt = DateTime.UtcNow.AddDays(1),
        InterviewLocation = "Phòng phỏng vấn"
    };

    private static object StudentState(Student student) => new
    {
        student.Status,
        student.CompanyId
    };

    private static PlacementRequest CreateRequest(
        int id,
        Company company,
        Student student,
        JobPosition position,
        RequestStatus status,
        DateTime createdAt) => new()
    {
        Id = id,
        CompanyId = company.Id,
        Company = company,
        StudentId = student.Id,
        Student = student,
        JobPositionId = position.Id,
        JobPosition = position,
        Status = status,
        CreatedAt = createdAt,
        DueAt = createdAt.AddDays(7),
        ApplicantFullName = "Ứng viên",
        ApplicantEmail = "applicant@example.test",
        ApplicantPhone = "0900000000"
    };

    private static async Task<TestFixture> SeedAsync(AppDbContext db)
    {
        var companyA = CreateCompany(1);
        var companyB = CreateCompany(2);
        var positionA = CreatePosition(11, companyA);
        var positionB = CreatePosition(22, companyB);
        var studentA = CreateStudent(1);
        var studentB = CreateStudent(2);

        db.AddRange(companyA, companyB, positionA, positionB, studentA, studentB);
        await db.SaveChangesAsync();
        return new TestFixture(companyA, companyB, positionA, positionB, studentA, studentB);
    }

    private static Company CreateCompany(int id) => new()
    {
        Id = id,
        Name = $"Company {id}",
        Address = "Company address",
        Industry = "Technology",
        ContactPerson = "Contact"
    };

    private static JobPosition CreatePosition(int id, Company company) => new()
    {
        Id = id,
        CompanyId = company.Id,
        Company = company,
        Title = $"Position {id}",
        Department = "Engineering",
        Quantity = 1,
        Description = "Position description",
        IsOpen = true
    };

    private static Student CreateStudent(int id) => new()
    {
        Id = id,
        StudentCode = $"SV{id:0000}",
        FullName = $"Student {id}",
        Major = "Software Engineering",
        ClassName = "SE01",
        Email = $"student{id}@example.test",
        PhoneNumber = "0911111111",
        Status = StudentStatus.NoCompany,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static PlacementRequestService CreateService(AppDbContext db) =>
        new(db, NullLogger<PlacementRequestService>.Instance, new ConfigurationBuilder().Build());

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed record TestFixture(
        Company CompanyA,
        Company CompanyB,
        JobPosition PositionA,
        JobPosition PositionB,
        Student StudentA,
        Student StudentB);
}
