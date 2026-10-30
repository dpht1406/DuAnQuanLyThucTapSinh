using FluentAssertions;
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

public class AdminPlacementFlowTests
{
    [Fact]
    public async Task Admin_passed_confirms_request_student_history_and_other_open_requests()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(10, fixture.Student, fixture.Company, fixture.Position, RequestStatus.InterviewInvited);
        request.StudentReportedInterviewAt = DateTime.UtcNow.AddDays(-1);
        var otherPending = CreateRequest(11, fixture.Student, fixture.Company2, fixture.Position2, RequestStatus.Pending);
        var otherInterview = CreateRequest(12, fixture.Student, fixture.Company2, fixture.Position2, RequestStatus.InterviewInvited);
        db.PlacementRequests.AddRange(request, otherPending, otherInterview);
        await db.SaveChangesAsync();

        var result = await CreateService(db).AdminConfirmResultAsync(10, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Passed,
            ConfirmationSource = "  Đã xác minh với doanh nghiệp  ",
            Note = "Đạt"
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        request.Status.Should().Be(RequestStatus.Accepted);
        request.ResultActorType.Should().Be(ActorType.Admin);
        request.ConfirmationSource.Should().Be("Đã xác minh với doanh nghiệp");
        fixture.Student.Status.Should().Be(StudentStatus.Accepted);
        fixture.Student.CompanyId.Should().Be(fixture.Company.Id);
        fixture.Student.JobPositionId.Should().Be(fixture.Position.Id);
        otherPending.Status.Should().Be(RequestStatus.Rejected);
        otherPending.ResultActorType.Should().Be(ActorType.System);
        otherInterview.Status.Should().Be(RequestStatus.Rejected);
        otherInterview.ResultActorType.Should().Be(ActorType.System);
        var history = await db.StatusHistories.SingleAsync();
        history.ChangedByType.Should().Be(ActorType.Admin);
        history.Note.Should().Contain("thay doanh nghiệp");
    }

    [Fact]
    public async Task Admin_passed_rejects_a_dismissed_report_without_changing_student_or_request_result()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = AddReportedRequest(db, fixture, 13);
        var service = CreateService(db);

        var dismissed = await service.AdminConfirmResultAsync(13, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.DismissReport,
            ConfirmationSource = "Đã xác minh"
        });
        var result = await service.AdminConfirmResultAsync(13, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Passed,
            ConfirmationSource = "Đã xác minh"
        });
        var acceptanceResult = await new PlacementAcceptanceService(
            db,
            NullLogger<PlacementAcceptanceService>.Instance).AcceptAsync(
                13,
                null,
                ActorType.Admin,
                99,
                "Đạt",
                "Đã xác minh");

        dismissed.Status.Should().Be(PlacementRequestOperationStatus.Success);
        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Báo cáo phỏng vấn này đã được xử lý.");
        acceptanceResult.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        acceptanceResult.Error.Should().Be("Báo cáo phỏng vấn này đã được xử lý.");
        request.Status.Should().Be(RequestStatus.InterviewInvited);
        request.ResultActorType.Should().BeNull();
        fixture.Student.Status.Should().Be(StudentStatus.NoCompany);
    }

    [Fact]
    public async Task Admin_passed_requires_a_student_report()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(14, fixture.Student, fixture.Company, fixture.Position, RequestStatus.InterviewInvited);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var result = await CreateService(db).AdminConfirmResultAsync(14, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Passed,
            ConfirmationSource = "Đã xác minh"
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        request.Status.Should().Be(RequestStatus.InterviewInvited);
        request.ResultActorType.Should().BeNull();
        fixture.Student.Status.Should().Be(StudentStatus.NoCompany);
    }

    [Fact]
    public async Task Admin_confirmation_requires_confirmation_source()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = AddReportedRequest(db, fixture, 20);

        var result = await CreateService(db).AdminConfirmResultAsync(20, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Failed,
            ConfirmationSource = null!,
            Note = "Không đạt"
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        request.Status.Should().Be(RequestStatus.InterviewInvited);
        request.ResultActorType.Should().BeNull();
    }

    [Fact]
    public async Task Admin_confirmation_rejects_confirmation_source_over_500_characters()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = AddReportedRequest(db, fixture, 21);

        var result = await CreateService(db).AdminConfirmResultAsync(21, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Failed,
            ConfirmationSource = new string('S', 501),
            Note = "Không đạt"
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        request.Status.Should().Be(RequestStatus.InterviewInvited);
    }

    [Fact]
    public async Task Admin_confirmation_rejects_note_over_1000_characters()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = AddReportedRequest(db, fixture, 22);

        var result = await CreateService(db).AdminConfirmResultAsync(22, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Failed,
            ConfirmationSource = "Đã xác minh",
            Note = new string('N', 1001)
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        request.Status.Should().Be(RequestStatus.InterviewInvited);
    }

    [Fact]
    public async Task Admin_failed_requires_note()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = AddReportedRequest(db, fixture, 23);

        var result = await CreateService(db).AdminConfirmResultAsync(23, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Failed,
            ConfirmationSource = "Đã xác minh",
            Note = "  "
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        request.Status.Should().Be(RequestStatus.InterviewInvited);
    }

    [Fact]
    public async Task Admin_failed_rejects_request_without_changing_student_company()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = AddReportedRequest(db, fixture, 24);

        var result = await CreateService(db).AdminConfirmResultAsync(24, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Failed,
            ConfirmationSource = "  Đã xác minh  ",
            Note = "  Chưa đạt yêu cầu.  "
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        request.Status.Should().Be(RequestStatus.Rejected);
        request.ResultActorType.Should().Be(ActorType.Admin);
        request.ConfirmationSource.Should().Be("Đã xác minh");
        fixture.Student.Status.Should().Be(StudentStatus.NoCompany);
        fixture.Student.CompanyId.Should().BeNull();
        fixture.Student.JobPositionId.Should().BeNull();
    }

    [Fact]
    public async Task Dismiss_report_preserves_interview_status_and_company_can_still_record_result()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = AddReportedRequest(db, fixture, 25);
        var service = CreateService(db);

        var dismissed = await service.AdminConfirmResultAsync(25, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.DismissReport,
            ConfirmationSource = "Đã xác minh"
        });

        dismissed.Status.Should().Be(PlacementRequestOperationStatus.Success);
        request.Status.Should().Be(RequestStatus.InterviewInvited);
        request.StudentReportDismissedAt.Should().NotBeNull();
        request.StudentReportDismissSource.Should().Be("Đã xác minh");
        request.StudentReportDismissNote.Should().Be("Báo cáo phỏng vấn đã bị Admin bác bỏ.");
        request.ResultActorType.Should().BeNull();
        request.ReviewedAt.Should().BeNull();
        request.ReviewedBy.Should().BeNull();
        request.ConfirmationSource.Should().BeNull();
        (await service.GetPendingConfirmationAsync(new PlacementRequestFilterDto { PageSize = 20 }))
            .Items.Should().BeEmpty();

        var secondDismissal = await service.AdminConfirmResultAsync(25, 99, new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.DismissReport,
            ConfirmationSource = "Đã xác minh"
        });
        secondDismissal.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);

        var companyResult = await service.RecordCompanyResultAsync(
            fixture.Company.Id,
            25,
            new CompanyPlacementResultDto { IsPassed = false, Note = "Không đạt sau phỏng vấn." },
            88);
        companyResult.Status.Should().Be(PlacementRequestOperationStatus.Success);
        request.Status.Should().Be(RequestStatus.Rejected);
        request.ResultActorType.Should().Be(ActorType.Company);
    }

    [Fact]
    public async Task Admin_confirmation_requires_student_report_and_returns_not_found_for_unknown_id()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = CreateRequest(26, fixture.Student, fixture.Company, fixture.Position, RequestStatus.InterviewInvited);
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var dto = new AdminPlacementResultDto
        {
            Decision = AdminPlacementDecision.Failed,
            ConfirmationSource = "Đã xác minh",
            Note = "Không đạt"
        };

        var noReport = await service.AdminConfirmResultAsync(26, 99, dto);
        var missing = await service.AdminConfirmResultAsync(999, 99, dto);

        noReport.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        request.Status.Should().Be(RequestStatus.InterviewInvited);
        missing.Status.Should().Be(PlacementRequestOperationStatus.NotFound);
    }

    [Fact]
    public async Task Admin_confirmation_rejects_decision_outside_enum()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var request = AddReportedRequest(db, fixture, 27);

        var result = await CreateService(db).AdminConfirmResultAsync(27, 99, new AdminPlacementResultDto
        {
            Decision = (AdminPlacementDecision)99,
            ConfirmationSource = "Đã xác minh"
        });

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        request.Status.Should().Be(RequestStatus.InterviewInvited);
    }

    [Fact]
    public async Task Pending_confirmation_returns_only_reported_undismissed_interview_invites()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var reported = AddReportedRequest(db, fixture, 30);
        var unreported = CreateRequest(31, fixture.Student, fixture.Company, fixture.Position, RequestStatus.InterviewInvited);
        db.PlacementRequests.Add(unreported);
        var pending = AddReportedRequest(db, fixture, 32, RequestStatus.Pending);
        var dismissed = AddReportedRequest(db, fixture, 33);
        dismissed.StudentReportDismissedAt = DateTime.UtcNow;
        var accepted = AddReportedRequest(db, fixture, 34, RequestStatus.Accepted);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetPendingConfirmationAsync(new PlacementRequestFilterDto { PageSize = 20 });

        result.Items.Select(item => item.Id).Should().Equal(reported.Id);
        unreported.StudentReportedInterviewAt.Should().BeNull();
        pending.StudentReportedInterviewAt.Should().NotBeNull();
        accepted.Status.Should().Be(RequestStatus.Accepted);
    }

    [Fact]
    public async Task Cancel_accepted_clears_student_assignment_and_records_admin_history()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 1, isOpen: false);
        fixture.Student.UpdatedAt = DateTime.UtcNow.AddDays(-2);
        await db.SaveChangesAsync();
        var oldUpdatedAt = fixture.Student.UpdatedAt;

        var result = await CreateService(db).CancelAcceptedResultAsync(40, 99, "Đã xác minh lại.");

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        fixture.Student.Status.Should().Be(StudentStatus.NoCompany);
        fixture.Student.CompanyId.Should().BeNull();
        fixture.Student.JobPositionId.Should().BeNull();
        fixture.Student.UpdatedAt.Should().BeAfter(oldUpdatedAt);
        fixture.Request.Status.Should().Be(RequestStatus.Rejected);
        fixture.Request.ResultActorType.Should().Be(ActorType.Admin);
        fixture.Request.ConfirmationSource.Should().Be("admin-cancel-accepted");
        fixture.Request.ReviewedBy.Should().Be(99);
        var history = await db.StatusHistories.SingleAsync();
        history.FromStatus.Should().Be(StudentStatus.Accepted);
        history.ToStatus.Should().Be(StudentStatus.NoCompany);
        history.ChangedByType.Should().Be(ActorType.Admin);
        history.ChangedBy.Should().Be(99);
    }

    [Fact]
    public async Task Cancel_reopens_closed_full_approved_position_after_removing_canceled_student()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 1, isOpen: false);

        var result = await CreateService(db).CancelAcceptedResultAsync(40, 99);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        fixture.Position.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_reopens_position_when_one_of_two_accepted_students_leaves_quantity_two()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 2, isOpen: false);
        AddAcceptedStudent(db, 2, fixture.Company, fixture.Position);
        await db.SaveChangesAsync();

        var result = await CreateService(db).CancelAcceptedResultAsync(40, 99);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        fixture.Position.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_does_not_reopen_when_remaining_active_students_fill_quantity()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 1, isOpen: false);
        AddAcceptedStudent(db, 2, fixture.Company, fixture.Position, StudentStatus.Interning);
        await db.SaveChangesAsync();

        var result = await CreateService(db).CancelAcceptedResultAsync(40, 99);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        fixture.Position.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Cancel_does_not_reopen_position_unless_it_is_approved_and_closed()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 1, isOpen: false);
        fixture.Position.ApprovalStatus = JobPositionApprovalStatus.PendingApproval;

        var result = await CreateService(db).CancelAcceptedResultAsync(40, 99);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        fixture.Position.IsOpen.Should().BeFalse();

        await using var openDb = CreateDbContext();
        var openFixture = await SeedAcceptedAsync(openDb, quantity: 1, isOpen: true);
        var openResult = await CreateService(openDb).CancelAcceptedResultAsync(40, 99);
        openResult.Status.Should().Be(PlacementRequestOperationStatus.Success);
        openFixture.Position.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_without_job_position_succeeds()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 1, isOpen: true, withPosition: false);

        var result = await CreateService(db).CancelAcceptedResultAsync(40, 99);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        fixture.Student.Status.Should().Be(StudentStatus.NoCompany);
        fixture.Request.JobPositionId.Should().BeNull();
        (await db.StatusHistories.ToListAsync()).Should().ContainSingle();
    }

    [Theory]
    [InlineData(StudentStatus.Interning, false)]
    [InlineData(StudentStatus.Accepted, true)]
    public async Task Cancel_rejects_interning_or_soft_deleted_student_without_mutating_data(
        StudentStatus status,
        bool isDeleted)
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 1, isOpen: false);
        fixture.Student.Status = status;
        fixture.Student.IsDeleted = isDeleted;
        await db.SaveChangesAsync();

        var result = await CreateService(db).CancelAcceptedResultAsync(40, 99);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        fixture.Request.Status.Should().Be(RequestStatus.Accepted);
        fixture.Student.Status.Should().Be(status);
        fixture.Student.CompanyId.Should().Be(fixture.Company.Id);
        fixture.Position.IsOpen.Should().BeFalse();
        (await db.StatusHistories.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Cancel_rejects_non_accepted_request_and_student_assignment_mismatches()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 1, isOpen: false);
        fixture.Request.Status = RequestStatus.Rejected;
        await db.SaveChangesAsync();
        var notAccepted = await CreateService(db).CancelAcceptedResultAsync(40, 99);
        notAccepted.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        fixture.Student.Status.Should().Be(StudentStatus.Accepted);
        fixture.Position.IsOpen.Should().BeFalse();

        fixture.Request.Status = RequestStatus.Accepted;
        fixture.Student.CompanyId = fixture.Company2.Id;
        await db.SaveChangesAsync();
        var companyMismatch = await CreateService(db).CancelAcceptedResultAsync(40, 99);
        companyMismatch.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);

        fixture.Student.CompanyId = fixture.Company.Id;
        fixture.Student.JobPositionId = 999;
        await db.SaveChangesAsync();
        var positionMismatch = await CreateService(db).CancelAcceptedResultAsync(40, 99);
        positionMismatch.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        fixture.Request.Status.Should().Be(RequestStatus.Accepted);
    }

    [Fact]
    public async Task Cancel_rejects_long_note_and_returns_not_found_for_missing_request()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAcceptedAsync(db, quantity: 1, isOpen: false);

        var longNote = await CreateService(db).CancelAcceptedResultAsync(40, 99, new string('N', 1001));
        var missing = await CreateService(db).CancelAcceptedResultAsync(999, 99);

        longNote.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        missing.Status.Should().Be(PlacementRequestOperationStatus.NotFound);
        fixture.Request.Status.Should().Be(RequestStatus.Accepted);
        fixture.Student.Status.Should().Be(StudentStatus.Accepted);
        fixture.Position.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Overdue_requests_filter_status_and_due_date_and_sort_earliest_due_first()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var now = DateTime.UtcNow;
        var older = CreateRequest(50, fixture.Student, fixture.Company, null, RequestStatus.Pending);
        older.CreatedAt = now.AddDays(-2);
        older.DueAt = now.AddDays(-8);
        var newer = CreateRequest(51, fixture.Student, fixture.Company, null, RequestStatus.Pending);
        newer.CreatedAt = now.AddDays(-20);
        newer.DueAt = now.AddDays(-6);
        var fresh = CreateRequest(52, fixture.Student, fixture.Company, null, RequestStatus.Pending);
        fresh.DueAt = now.AddDays(6);
        var excluded = new[]
        {
            CreateRequest(53, fixture.Student, fixture.Company, null, RequestStatus.InterviewInvited),
            CreateRequest(54, fixture.Student, fixture.Company, null, RequestStatus.Expired),
            CreateRequest(55, fixture.Student, fixture.Company, null, RequestStatus.Rejected),
            CreateRequest(56, fixture.Student, fixture.Company, null, RequestStatus.Accepted)
        };
        foreach (var item in excluded)
        {
            item.CreatedAt = now.AddDays(-20);
            item.DueAt = now.AddDays(-10);
        }
        db.PlacementRequests.AddRange(older, newer, fresh);
        db.PlacementRequests.AddRange(excluded);
        await db.SaveChangesAsync();

        var result = await CreateDashboard(db, ("OverdueRequestAlertDays", "5")).GetOverdueRequestsAsync();

        result.Select(item => item.Id).Should().Equal(50, 51);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData(null)]
    public async Task Overdue_requests_use_default_five_days_for_invalid_or_missing_configuration(string? days)
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var now = DateTime.UtcNow;
        var oldEnough = CreateRequest(57, fixture.Student, fixture.Company, null);
        oldEnough.CreatedAt = now;
        oldEnough.DueAt = now.AddDays(-6);
        var tooNew = CreateRequest(58, fixture.Student, fixture.Company, null);
        tooNew.CreatedAt = now.AddDays(-20);
        tooNew.DueAt = now.AddDays(6);
        db.PlacementRequests.AddRange(oldEnough, tooNew);
        await db.SaveChangesAsync();
        (string Key, string? Value)[] config = days is null
            ? Array.Empty<(string Key, string? Value)>()
            : new[] { ("OverdueRequestAlertDays", (string?)days) };

        var result = await CreateDashboard(db, config).GetOverdueRequestsAsync();

        result.Select(item => item.Id).Should().Equal(57);
    }

    [Fact]
    public async Task Pending_interview_results_include_dismissed_reports_and_sort_oldest_first_without_mutation()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var oldest = CreateRequest(60, fixture.Student, fixture.Company, null, RequestStatus.InterviewInvited);
        oldest.InterviewAt = DateTime.UtcNow.AddDays(-10);
        oldest.StudentReportDismissedAt = DateTime.UtcNow.AddDays(-1);
        var second = CreateRequest(61, fixture.Student, fixture.Company, null, RequestStatus.InterviewInvited);
        second.InterviewAt = DateTime.UtcNow.AddDays(-8);
        second.StudentReportedInterviewAt = DateTime.UtcNow.AddDays(-7);
        var tooNew = CreateRequest(62, fixture.Student, fixture.Company, null, RequestStatus.InterviewInvited);
        tooNew.InterviewAt = DateTime.UtcNow.AddDays(-1);
        var future = CreateRequest(63, fixture.Student, fixture.Company, null, RequestStatus.InterviewInvited);
        future.InterviewAt = DateTime.UtcNow.AddDays(2);
        var missingDate = CreateRequest(64, fixture.Student, fixture.Company, null, RequestStatus.InterviewInvited);
        var pending = CreateRequest(65, fixture.Student, fixture.Company, null, RequestStatus.Pending);
        pending.InterviewAt = DateTime.UtcNow.AddDays(-10);
        db.PlacementRequests.AddRange(oldest, second, tooNew, future, missingDate, pending);
        await db.SaveChangesAsync();

        var before = (await db.PlacementRequests.Select(item => new { item.Id, item.Status }).ToListAsync())
            .ToDictionary(item => item.Id, item => item.Status);
        var result = await CreateDashboard(db, ("InterviewResultOverdueDays", "3"))
            .GetPendingInterviewResultsAsync();

        result.Select(item => item.Id).Should().Equal(60, 61);
        (await db.PlacementRequests.ToDictionaryAsync(item => item.Id, item => item.Status)).Should()
            .BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData(null)]
    public async Task Pending_interview_results_use_default_three_days_for_invalid_or_missing_configuration(
        string? days)
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var overdue = CreateRequest(66, fixture.Student, fixture.Company, null, RequestStatus.InterviewInvited);
        overdue.InterviewAt = DateTime.UtcNow.AddDays(-4);
        var recent = CreateRequest(67, fixture.Student, fixture.Company, null, RequestStatus.InterviewInvited);
        recent.InterviewAt = DateTime.UtcNow.AddDays(-2);
        db.PlacementRequests.AddRange(overdue, recent);
        await db.SaveChangesAsync();
        (string Key, string? Value)[] config = days is null
            ? Array.Empty<(string Key, string? Value)>()
            : new[] { ("InterviewResultOverdueDays", (string?)days) };

        var result = await CreateDashboard(db, config).GetPendingInterviewResultsAsync();

        result.Select(item => item.Id).Should().Equal(66);
        overdue.Status.Should().Be(RequestStatus.InterviewInvited);
        recent.Status.Should().Be(RequestStatus.InterviewInvited);
    }

    private static PlacementRequest AddReportedRequest(
        AppDbContext db,
        TestFixture fixture,
        int id,
        RequestStatus status = RequestStatus.InterviewInvited)
    {
        var request = CreateRequest(id, fixture.Student, fixture.Company, fixture.Position, status);
        request.StudentReportedInterviewAt = DateTime.UtcNow.AddDays(-1);
        db.PlacementRequests.Add(request);
        db.SaveChanges();
        return request;
    }

    private static async Task<TestFixture> SeedAsync(AppDbContext db)
    {
        var company = CreateCompany(1);
        var company2 = CreateCompany(2);
        var position = CreatePosition(1, company);
        var position2 = CreatePosition(2, company2);
        var student = CreateStudent(1);
        db.AddRange(company, company2, position, position2, student);
        await db.SaveChangesAsync();
        return new TestFixture(company, company2, position, position2, student);
    }

    private static async Task<AcceptedFixture> SeedAcceptedAsync(
        AppDbContext db,
        int quantity,
        bool isOpen,
        bool withPosition = true)
    {
        var fixture = await SeedAsync(db);
        fixture.Position.Quantity = quantity;
        fixture.Position.IsOpen = isOpen;
        var request = CreateRequest(
            40,
            fixture.Student,
            fixture.Company,
            withPosition ? fixture.Position : null,
            RequestStatus.Accepted);
        fixture.Student.Status = StudentStatus.Accepted;
        fixture.Student.CompanyId = fixture.Company.Id;
        fixture.Student.JobPositionId = withPosition ? fixture.Position.Id : null;
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();
        return new AcceptedFixture(fixture.Company, fixture.Company2, fixture.Position, fixture.Student, request);
    }

    private static void AddAcceptedStudent(
        AppDbContext db,
        int id,
        Company company,
        JobPosition position,
        StudentStatus status = StudentStatus.Accepted)
    {
        var student = CreateStudent(id);
        student.Status = status;
        student.CompanyId = company.Id;
        student.JobPositionId = position.Id;
        db.Students.Add(student);
    }

    private static PlacementRequestService CreateService(AppDbContext db) =>
        new(
            db,
            NullLogger<PlacementRequestService>.Instance,
            new ConfigurationBuilder().Build(),
            new PlacementAcceptanceService(db, NullLogger<PlacementAcceptanceService>.Instance));

    private static DashboardService CreateDashboard(AppDbContext db, params (string Key, string? Value)[] settings)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(item => item.Key, item => item.Value))
            .Build();
        return new DashboardService(db, config);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
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

    private static PlacementRequest CreateRequest(
        int id,
        Student student,
        Company company,
        JobPosition? position,
        RequestStatus status = RequestStatus.Pending) => new()
    {
        Id = id,
        StudentId = student.Id,
        Student = student,
        CompanyId = company.Id,
        Company = company,
        JobPositionId = position?.Id,
        JobPosition = position,
        Status = status,
        CreatedAt = DateTime.UtcNow,
        DueAt = DateTime.UtcNow.AddDays(7)
    };

    private sealed record TestFixture(
        Company Company,
        Company Company2,
        JobPosition Position,
        JobPosition Position2,
        Student Student);

    private sealed record AcceptedFixture(
        Company Company,
        Company Company2,
        JobPosition Position,
        Student Student,
        PlacementRequest Request);
}
