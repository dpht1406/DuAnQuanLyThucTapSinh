using System.Reflection;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Api.Controllers;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class CompanyPlacementDecisionTests
{
    [Fact]
    public async Task Passing_request_accepts_student_and_rejects_other_open_requests()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var acceptedRequest = CreateRequest(101, fixture.CompanyA, fixture.StudentA, fixture.PositionA);
        var otherRequest = CreateRequest(102, fixture.CompanyB, fixture.StudentA, fixture.PositionB);
        var thirdRequest = CreateRequest(103, fixture.CompanyA, fixture.StudentA, fixture.PositionA);
        db.PlacementRequests.AddRange(acceptedRequest, otherRequest, thirdRequest);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            acceptedRequest.Id,
            new CompanyPlacementResultDto { IsPassed = true, Note = "  Đạt tốt.  " },
            fixture.CompanyAUser.Id);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        acceptedRequest.Status.Should().Be(RequestStatus.Accepted);
        acceptedRequest.ResultNote.Should().Be("Đạt tốt.");
        acceptedRequest.ResultActorType.Should().Be(ActorType.Company);
        acceptedRequest.ReviewedBy.Should().Be(fixture.CompanyAUser.Id);
        acceptedRequest.ReviewedAt.Should().NotBeNull();
        otherRequest.Status.Should().Be(RequestStatus.Rejected);
        otherRequest.ResultActorType.Should().Be(ActorType.System);
        otherRequest.ResultNote.Should().Be("Sinh viên đã được doanh nghiệp khác nhận");
        otherRequest.ReviewedBy.Should().BeNull();
        otherRequest.ReviewedAt.Should().NotBeNull();
        thirdRequest.Status.Should().Be(RequestStatus.Rejected);
        fixture.StudentA.Status.Should().Be(StudentStatus.Accepted);
        fixture.StudentA.CompanyId.Should().Be(fixture.CompanyA.Id);
        fixture.StudentA.JobPositionId.Should().Be(fixture.PositionA.Id);

        var history = await db.StatusHistories.ToListAsync();
        history.Should().ContainSingle();
        history[0].FromStatus.Should().Be(StudentStatus.NoCompany);
        history[0].ToStatus.Should().Be(StudentStatus.Accepted);
        history[0].CompanyId.Should().Be(fixture.CompanyA.Id);
        history[0].ChangedBy.Should().Be(fixture.CompanyAUser.Id);
        history[0].ChangedByType.Should().Be(ActorType.Company);
        history[0].Note.Should().Contain(fixture.PositionA.Title);

        var notification = await db.Notifications.SingleAsync();
        notification.Type.Should().Be(NotificationType.StudentAcceptedByCompany);
        notification.Title.Should().Be("Doanh nghiệp nhận sinh viên");
        notification.Message.Should().Contain(fixture.StudentA.FullName);
        notification.Message.Should().Contain(fixture.StudentA.StudentCode);
        notification.Message.Should().Contain(fixture.CompanyA.Name);
        notification.Message.Should().Contain(fixture.PositionA.Title);
        notification.StudentId.Should().Be(fixture.StudentA.Id);
        notification.CompanyId.Should().Be(fixture.CompanyA.Id);
        notification.CompanyName.Should().Be(fixture.CompanyA.Name);
    }

    [Fact]
    public async Task Company_can_pass_an_interview_invite_without_a_student_report()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(
            104,
            fixture.CompanyA,
            fixture.StudentA,
            fixture.PositionA,
            RequestStatus.InterviewInvited);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            request.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyAUser.Id);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        request.StudentReportedInterviewAt.Should().BeNull();
        request.Status.Should().Be(RequestStatus.Accepted);
        fixture.StudentA.Status.Should().Be(StudentStatus.Accepted);
    }

    [Fact]
    public async Task Another_company_cannot_accept_a_student_already_accepted()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var requestA = CreateRequest(201, fixture.CompanyA, fixture.StudentA, fixture.PositionA);
        db.PlacementRequests.Add(requestA);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var first = await service.RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            requestA.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyAUser.Id);
        var historyCount = await db.StatusHistories.CountAsync();

        var requestB = CreateRequest(202, fixture.CompanyB, fixture.StudentA, fixture.PositionB);
        db.PlacementRequests.Add(requestB);
        await db.SaveChangesAsync();

        var second = await service.RecordCompanyResultAsync(
            fixture.CompanyB.Id,
            requestB.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyBUser.Id);

        first.Status.Should().Be(PlacementRequestOperationStatus.Success);
        second.Status.Should().Be(PlacementRequestOperationStatus.Conflict);
        second.Error.Should().Be("Sinh viên đã được doanh nghiệp khác nhận hoặc đang được Admin xử lý.");
        requestB.Status.Should().Be(RequestStatus.Pending);
        requestB.ResultActorType.Should().BeNull();
        (await db.StatusHistories.CountAsync()).Should().Be(historyCount);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task Acceptance_enforces_position_quantity_and_closes_when_full(int quantity, bool secondCanPass)
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        fixture.PositionA.Quantity = quantity;
        var thirdStudent = CreateStudent(3);
        db.Students.Add(thirdStudent);
        var firstRequest = CreateRequest(301, fixture.CompanyA, fixture.StudentA, fixture.PositionA);
        var secondRequest = CreateRequest(302, fixture.CompanyA, thirdStudent, fixture.PositionA);
        db.PlacementRequests.AddRange(firstRequest, secondRequest);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var first = await service.RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            firstRequest.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyAUser.Id);
        fixture.PositionA.IsOpen.Should().Be(quantity != 1);

        var second = await service.RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            secondRequest.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyAUser.Id);

        first.Status.Should().Be(PlacementRequestOperationStatus.Success);
        if (secondCanPass)
        {
            second.Status.Should().Be(PlacementRequestOperationStatus.Success);
            fixture.PositionA.IsOpen.Should().BeFalse();
        }
        else
        {
            second.Status.Should().Be(PlacementRequestOperationStatus.Conflict);
            second.Error.Should().Be("Vị trí đã đủ chỉ tiêu.");
            secondRequest.Status.Should().Be(RequestStatus.Pending);
        }
    }

    [Fact]
    public async Task Foreign_company_result_returns_not_found_without_mutation()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(401, fixture.CompanyA, fixture.StudentA, fixture.PositionA);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordCompanyResultAsync(
            fixture.CompanyB.Id,
            request.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyBUser.Id);

        result.Status.Should().Be(PlacementRequestOperationStatus.NotFound);
        request.Status.Should().Be(RequestStatus.Pending);
        fixture.StudentA.Status.Should().Be(StudentStatus.NoCompany);
        (await db.StatusHistories.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Passing_without_a_job_position_does_not_change_any_position()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(501, fixture.CompanyA, fixture.StudentA, position: null);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            request.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyAUser.Id);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        fixture.StudentA.JobPositionId.Should().BeNull();
        fixture.PositionA.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task Student_not_in_no_company_state_causes_conflict()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        fixture.StudentA.Status = StudentStatus.Introduced;
        var request = CreateRequest(601, fixture.CompanyA, fixture.StudentA, fixture.PositionA);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            request.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyAUser.Id);

        result.Status.Should().Be(PlacementRequestOperationStatus.Conflict);
        result.Error.Should().Be("Sinh viên đã được doanh nghiệp khác nhận hoặc đang được Admin xử lý.");
        request.Status.Should().Be(RequestStatus.Pending);
        (await db.StatusHistories.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Unapproved_position_is_a_business_error()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        fixture.PositionA.ApprovalStatus = JobPositionApprovalStatus.PendingApproval;
        var request = CreateRequest(701, fixture.CompanyA, fixture.StudentA, fixture.PositionA);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            request.Id,
            new CompanyPlacementResultDto { IsPassed = true },
            fixture.CompanyAUser.Id);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Vị trí tuyển dụng chưa được duyệt.");
        request.Status.Should().Be(RequestStatus.Pending);
        (await db.StatusHistories.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Company_can_record_not_pass_without_changing_student_or_other_requests()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var rejectedRequest = CreateRequest(801, fixture.CompanyA, fixture.StudentA, fixture.PositionA);
        var otherRequest = CreateRequest(802, fixture.CompanyB, fixture.StudentA, fixture.PositionB);
        db.PlacementRequests.AddRange(rejectedRequest, otherRequest);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            rejectedRequest.Id,
            new CompanyPlacementResultDto { IsPassed = false, Note = "  Chưa phù hợp.  " },
            fixture.CompanyAUser.Id);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        rejectedRequest.Status.Should().Be(RequestStatus.Rejected);
        rejectedRequest.ResultNote.Should().Be("Chưa phù hợp.");
        rejectedRequest.ResultActorType.Should().Be(ActorType.Company);
        rejectedRequest.ReviewedBy.Should().Be(fixture.CompanyAUser.Id);
        rejectedRequest.ReviewedAt.Should().NotBeNull();
        otherRequest.Status.Should().Be(RequestStatus.Pending);
        fixture.StudentA.Status.Should().Be(StudentStatus.NoCompany);
        fixture.StudentA.CompanyId.Should().BeNull();
        fixture.StudentA.JobPositionId.Should().BeNull();
        (await db.StatusHistories.ToListAsync()).Should().BeEmpty();
        (await db.Notifications.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public void Result_validator_requires_a_note_for_not_pass_and_enforces_trimmed_length()
    {
        var validator = new CompanyPlacementResultDtoValidator();

        validator.TestValidate(new CompanyPlacementResultDto { IsPassed = false, Note = "  " })
            .ShouldHaveValidationErrorFor(result => result.Note)
            .WithErrorMessage("Ghi chú là bắt buộc khi sinh viên không đạt.");
        validator.TestValidate(new CompanyPlacementResultDto { IsPassed = true, Note = new string('x', 1001) })
            .ShouldHaveValidationErrorFor(result => result.Note)
            .WithErrorMessage("Ghi chú kết quả không được vượt quá 1000 ký tự.");
        validator.TestValidate(new CompanyPlacementResultDto { IsPassed = true, Note = null })
            .ShouldNotHaveValidationErrorFor(result => result.Note);
    }

    [Theory]
    [InlineData(RequestStatus.Accepted)]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Expired)]
    public async Task Finalized_request_cannot_record_result_again(RequestStatus status)
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(901, fixture.CompanyA, fixture.StudentA, fixture.PositionA, status);
        request.ResultNote = "Kết quả trước";
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordCompanyResultAsync(
            fixture.CompanyA.Id,
            request.Id,
            new CompanyPlacementResultDto { IsPassed = false, Note = "Kết quả mới" },
            fixture.CompanyAUser.Id);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Đơn này đã được xử lý.");
        request.Status.Should().Be(status);
        request.ResultNote.Should().Be("Kết quả trước");
    }

    [Fact]
    public void Result_endpoint_is_company_authorized()
    {
        var action = typeof(CompanyMeController).GetMethod(nameof(CompanyMeController.RecordPlacementResult));

        action.Should().NotBeNull();
        action!.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be("placement-requests/{id:int}/result");
        typeof(CompanyMeController).GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be("Company");
    }

    private static PlacementRequestService CreateService(AppDbContext db) =>
        new(
            db,
            NullLogger<PlacementRequestService>.Instance,
            new ConfigurationBuilder().Build(),
            new PlacementAcceptanceService(db, NullLogger<PlacementAcceptanceService>.Instance));

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<TestFixture> SeedAsync(AppDbContext db)
    {
        var companyA = CreateCompany(1);
        var companyB = CreateCompany(2);
        var positionA = CreatePosition(11, companyA);
        var positionB = CreatePosition(22, companyB);
        var studentA = CreateStudent(1);
        var studentB = CreateStudent(2);
        var companyAUser = CreateUser(77, companyA);
        var companyBUser = CreateUser(88, companyB);

        db.AddRange(
            companyA,
            companyB,
            positionA,
            positionB,
            studentA,
            studentB,
            companyAUser,
            companyBUser);
        await db.SaveChangesAsync();
        return new TestFixture(
            companyA,
            companyB,
            positionA,
            positionB,
            studentA,
            studentB,
            companyAUser,
            companyBUser);
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
        Quantity = 3,
        Description = "Position description",
        IsOpen = true,
        ApprovalStatus = JobPositionApprovalStatus.Approved
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

    private static User CreateUser(int id, Company company) => new()
    {
        Id = id,
        Username = $"company-user-{id}",
        PasswordHash = "hash",
        Role = UserRole.Company,
        CompanyId = company.Id,
        Company = company,
        CreatedAt = DateTime.UtcNow
    };

    private static PlacementRequest CreateRequest(
        int id,
        Company company,
        Student student,
        JobPosition? position,
        RequestStatus status = RequestStatus.Pending) => new()
    {
        Id = id,
        CompanyId = company.Id,
        Company = company,
        StudentId = student.Id,
        Student = student,
        JobPositionId = position?.Id,
        JobPosition = position,
        Status = status,
        CreatedAt = DateTime.UtcNow,
        DueAt = DateTime.UtcNow.AddDays(7)
    };

    private sealed record TestFixture(
        Company CompanyA,
        Company CompanyB,
        JobPosition PositionA,
        JobPosition PositionB,
        Student StudentA,
        Student StudentB,
        User CompanyAUser,
        User CompanyBUser);
}
