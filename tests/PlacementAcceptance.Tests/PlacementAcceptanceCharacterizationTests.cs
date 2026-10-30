using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;
using Xunit;

namespace PlacementAcceptance.Tests;

public class PlacementAcceptanceCharacterizationTests
{
    private static AppDbContext CreateDbContext(params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        var options = optionsBuilder.Options;
        return new AppDbContext(options);
    }

    private static PlacementAcceptanceService CreateService(AppDbContext db) =>
        new(db, NullLogger<PlacementAcceptanceService>.Instance);

    private static PlacementAcceptanceService CreateService(
        AppDbContext db,
        PlacementRequestEmailNotifier emailNotifier) =>
        new(db, NullLogger<PlacementAcceptanceService>.Instance, emailNotifier);

    private static async Task<(Company Company, JobPosition Position, Student Student, User AdminUser, User CompanyUser)> SeedBaseEntitiesAsync(
        AppDbContext db,
        int quota = 2)
    {
        var adminUser = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "hash",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        var companyUser = new User
        {
            Id = 2,
            Username = "company_user",
            PasswordHash = "hash",
            Role = UserRole.Company,
            CreatedAt = DateTime.UtcNow
        };

        var company = new Company
        {
            Id = 10,
            Name = "Acme Corp",
            Address = "123 Tech Street",
            Industry = "IT",
            ContactPerson = "HR Manager",
            ContactEmail = "hr@acme.com",
            ContactPhone = "0123456789"
        };

        var position = new JobPosition
        {
            Id = 20,
            CompanyId = company.Id,
            Company = company,
            Title = ".NET Developer Intern",
            Department = "Engineering",
            Description = "Backend intern",
            Quantity = quota,
            IsOpen = true,
            ApprovalStatus = JobPositionApprovalStatus.Approved
        };

        var student = new Student
        {
            Id = 100,
            StudentCode = "SV001",
            FullName = "Nguyen Van A",
            Email = "sv001@example.com",
            PhoneNumber = "0987654321",
            ClassName = "CNTT1",
            Major = "CNTT",
            Status = StudentStatus.NoCompany,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.AddRange(adminUser, companyUser);
        db.Companies.Add(company);
        db.JobPositions.Add(position);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        return (company, position, student, adminUser, companyUser);
    }

    // -------------------------------------------------------------
    // AcceptAsync Tests (12 tests)
    // -------------------------------------------------------------

    [Fact]
    public async Task AcceptAsync_HappyPath_CompanyActor_SucceedsAndUpdatesStudentAndSideEffects()
    {
        await using var db = CreateDbContext();
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db);

        var request = new PlacementRequest
        {
            Id = 501,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Another open request for the same student that should be auto-rejected
        var otherCompany = new Company
        {
            Id = 11,
            Name = "Other Corp",
            Address = "456 Other St",
            Industry = "Software",
            ContactPerson = "Other HR"
        };
        db.Companies.Add(otherCompany);
        var otherRequest = new PlacementRequest
        {
            Id = 502,
            StudentId = student.Id,
            CompanyId = otherCompany.Id,
            Status = RequestStatus.InterviewInvited,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.AddRange(request, otherRequest);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: company.Id,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: "Pass interview with high score",
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        result.Error.Should().BeNull();

        // Verify request was updated
        var updatedReq = await db.PlacementRequests.FindAsync(request.Id);
        updatedReq!.Status.Should().Be(RequestStatus.Accepted);
        updatedReq.ResultActorType.Should().Be(ActorType.Company);
        updatedReq.ReviewedBy.Should().Be(companyUser.Id);
        updatedReq.ResultNote.Should().Be("Pass interview with high score");
        updatedReq.ReviewedAt.Should().NotBeNull();

        // Verify student was updated
        var updatedStudent = await db.Students.FindAsync(student.Id);
        updatedStudent!.Status.Should().Be(StudentStatus.Accepted);
        updatedStudent.CompanyId.Should().Be(company.Id);
        updatedStudent.JobPositionId.Should().Be(position.Id);

        // Verify other request was rejected by system
        var updatedOtherReq = await db.PlacementRequests.FindAsync(otherRequest.Id);
        updatedOtherReq!.Status.Should().Be(RequestStatus.Rejected);
        updatedOtherReq.ResultActorType.Should().Be(ActorType.System);
        updatedOtherReq.ResultNote.Should().Be("Sinh viên đã được doanh nghiệp khác nhận");

        // Verify StatusHistory was created
        var history = await db.StatusHistories.FirstOrDefaultAsync(h => h.StudentId == student.Id);
        history.Should().NotBeNull();
        history!.FromStatus.Should().Be(StudentStatus.NoCompany);
        history.ToStatus.Should().Be(StudentStatus.Accepted);
        history.CompanyId.Should().Be(company.Id);
        history.Note.Should().Be($"Doanh nghiệp xác nhận Đạt cho vị trí {position.Title}");

        // Verify Notification was created
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.StudentId == student.Id);
        notification.Should().NotBeNull();
        notification!.Type.Should().Be(NotificationType.StudentAcceptedByCompany);
    }

    [Fact]
    public async Task AcceptAsync_HappyPath_AdminActor_WithReportedInterview_Succeeds()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        var request = new PlacementRequest
        {
            Id = 503,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.InterviewInvited,
            StudentReportedInterviewAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: null,
            actor: ActorType.Admin,
            actorUserId: adminUser.Id,
            note: "Admin confirmed via phone",
            confirmationSource: "Phone call verification");

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);

        var updatedReq = await db.PlacementRequests.FindAsync(request.Id);
        updatedReq!.Status.Should().Be(RequestStatus.Accepted);
        updatedReq.ConfirmationSource.Should().Be("Phone call verification");

        var history = await db.StatusHistories.FirstOrDefaultAsync(h => h.StudentId == student.Id);
        history!.Note.Should().Be($"Admin xác nhận Đạt thay doanh nghiệp cho vị trí {position.Title}");
    }

    [Fact]
    public async Task AcceptAsync_HappyPath_WithoutJobPosition_SetsNullPositionAndCorrectHistoryNote()
    {
        await using var db = CreateDbContext();
        var (company, _, student, _, companyUser) = await SeedBaseEntitiesAsync(db);

        var request = new PlacementRequest
        {
            Id = 504,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = null,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: company.Id,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: null,
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);

        var updatedStudent = await db.Students.FindAsync(student.Id);
        updatedStudent!.JobPositionId.Should().BeNull();

        var history = await db.StatusHistories.FirstOrDefaultAsync(h => h.StudentId == student.Id);
        history!.Note.Should().Be("Doanh nghiệp xác nhận Đạt");
    }

    [Fact]
    public async Task AcceptAsync_AutoClosesJobPosition_WhenQuotaReached()
    {
        await using var db = CreateDbContext();
        // Quota is 1, so accepting 1 student will fill it completely
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db, quota: 1);

        var request = new PlacementRequest
        {
            Id = 505,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: company.Id,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: null,
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);

        var updatedPos = await db.JobPositions.FindAsync(position.Id);
        updatedPos!.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptAsync_WrongScopeCompany_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db);

        var request = new PlacementRequest
        {
            Id = 506,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        // Company ID 999 does not match request's CompanyId (10)
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: 999,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: null,
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.NotFound);
        result.Error.Should().Be("Không tìm thấy đơn ứng tuyển.");
    }

    [Fact]
    public async Task AcceptAsync_NonExistentRequest_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        await SeedBaseEntitiesAsync(db);

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: 99999,
            scopeCompanyId: null,
            actor: ActorType.Company,
            actorUserId: 2,
            note: null,
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.NotFound);
        result.Error.Should().Be("Không tìm thấy đơn ứng tuyển.");
    }

    [Fact]
    public async Task AcceptAsync_AdminActor_WhenReportNotInterviewInvited_ReturnsBusinessError()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        // Status is Pending, but Admin can only confirm InterviewInvited
        var request = new PlacementRequest
        {
            Id = 507,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: null,
            actor: ActorType.Admin,
            actorUserId: adminUser.Id,
            note: null,
            confirmationSource: "Test");

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Báo cáo phỏng vấn này đã được xử lý.");
    }

    [Fact]
    public async Task AcceptAsync_AdminActor_WhenReportDismissed_ReturnsBusinessError()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        var request = new PlacementRequest
        {
            Id = 508,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.InterviewInvited,
            StudentReportedInterviewAt = DateTime.UtcNow.AddDays(-2),
            StudentReportDismissedAt = DateTime.UtcNow.AddDays(-1), // Already dismissed
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: null,
            actor: ActorType.Admin,
            actorUserId: adminUser.Id,
            note: null,
            confirmationSource: "Test");

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Báo cáo phỏng vấn này đã được xử lý.");
    }

    [Fact]
    public async Task AcceptAsync_AlreadyProcessedRequest_ReturnsBusinessError()
    {
        await using var db = CreateDbContext();
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db);

        var request = new PlacementRequest
        {
            Id = 509,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Rejected, // Already processed
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: company.Id,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: null,
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Đơn này đã được xử lý.");
    }

    [Fact]
    public async Task AcceptAsync_StudentAlreadyAcceptedOrInterning_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db);

        student.Status = StudentStatus.Accepted; // Not NoCompany!
        student.CompanyId = company.Id;

        var request = new PlacementRequest
        {
            Id = 510,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: company.Id,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: null,
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.Conflict);
        result.Error.Should().Be("Sinh viên đã được doanh nghiệp khác nhận hoặc đang được Admin xử lý.");
    }

    [Fact]
    public async Task AcceptAsync_JobPositionCapacityExceeded_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        // Quota is 1, and 1 student is already accepted in this position
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db, quota: 1);

        var existingAcceptedStudent = new Student
        {
            Id = 101,
            StudentCode = "SV002",
            FullName = "Nguyen Van B",
            Email = "sv002@example.com",
            PhoneNumber = "0987654322",
            ClassName = "CNTT1",
            Major = "CNTT",
            Status = StudentStatus.Accepted,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Students.Add(existingAcceptedStudent);

        var request = new PlacementRequest
        {
            Id = 511,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: company.Id,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: null,
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.Conflict);
        result.Error.Should().Be("Vị trí đã đủ chỉ tiêu.");
    }

    [Fact]
    public async Task AcceptAsync_UnapprovedJobPosition_ReturnsBusinessError()
    {
        await using var db = CreateDbContext();
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db);

        position.ApprovalStatus = JobPositionApprovalStatus.PendingApproval; // Not Approved!

        var request = new PlacementRequest
        {
            Id = 512,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AcceptAsync(
            requestId: request.Id,
            scopeCompanyId: company.Id,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: null,
            confirmationSource: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Vị trí tuyển dụng chưa được duyệt.");
    }

    // -------------------------------------------------------------
    // CancelAcceptedAsync Tests (8 tests)
    // -------------------------------------------------------------

    [Fact]
    public async Task CancelAcceptedAsync_HappyPath_SucceedsAndRevertsStudentAndRequest()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        student.Status = StudentStatus.Accepted;
        student.CompanyId = company.Id;
        student.JobPositionId = position.Id;

        var request = new PlacementRequest
        {
            Id = 601,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CancelAcceptedAsync(
            requestId: request.Id,
            adminUserId: adminUser.Id,
            note: "Cancel due to student request");

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);
        result.Error.Should().BeNull();

        // Check student reverted to NoCompany
        var updatedStudent = await db.Students.FindAsync(student.Id);
        updatedStudent!.Status.Should().Be(StudentStatus.NoCompany);
        updatedStudent.CompanyId.Should().BeNull();
        updatedStudent.JobPositionId.Should().BeNull();

        // Check request changed to Rejected
        var updatedReq = await db.PlacementRequests.FindAsync(request.Id);
        updatedReq!.Status.Should().Be(RequestStatus.Rejected);
        updatedReq.ResultNote.Should().Be("Cancel due to student request");
        updatedReq.ResultActorType.Should().Be(ActorType.Admin);
        updatedReq.ReviewedBy.Should().Be(adminUser.Id);
        updatedReq.ConfirmationSource.Should().Be("admin-cancel-accepted");

        // Check StatusHistory
        var history = await db.StatusHistories.FirstOrDefaultAsync(h => h.StudentId == student.Id);
        history.Should().NotBeNull();
        history!.FromStatus.Should().Be(StudentStatus.Accepted);
        history.ToStatus.Should().Be(StudentStatus.NoCompany);
        history.CompanyId.Should().Be(company.Id);
        history.Note.Should().Be("Cancel due to student request");
    }

    [Fact]
    public async Task CancelAcceptedAsync_DefaultNote_WhenNoteNullOrWhitespace()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        student.Status = StudentStatus.Accepted;
        student.CompanyId = company.Id;
        student.JobPositionId = position.Id;

        var request = new PlacementRequest
        {
            Id = 602,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CancelAcceptedAsync(
            requestId: request.Id,
            adminUserId: adminUser.Id,
            note: "   ");

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);

        var updatedReq = await db.PlacementRequests.FindAsync(request.Id);
        updatedReq!.ResultNote.Should().Be("Admin hủy kết quả tiếp nhận sinh viên.");
    }

    [Fact]
    public async Task CancelAcceptedAsync_ReopensJobPosition_WhenClosedAndCapacityAvailable()
    {
        await using var db = CreateDbContext();
        // Quota is 1, position is closed because this student was accepted
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db, quota: 1);
        position.IsOpen = false;

        student.Status = StudentStatus.Accepted;
        student.CompanyId = company.Id;
        student.JobPositionId = position.Id;

        var request = new PlacementRequest
        {
            Id = 603,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CancelAcceptedAsync(
            requestId: request.Id,
            adminUserId: adminUser.Id,
            note: "Student withdrew");

        result.Status.Should().Be(PlacementRequestOperationStatus.Success);

        var updatedPos = await db.JobPositions.FindAsync(position.Id);
        updatedPos!.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task CancelAcceptedAsync_WhenRequestNotAccepted_ReturnsBusinessError()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        var request = new PlacementRequest
        {
            Id = 604,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending, // Not Accepted!
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CancelAcceptedAsync(
            requestId: request.Id,
            adminUserId: adminUser.Id,
            note: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Chỉ có thể hủy kết quả tiếp nhận khi đơn đang ở trạng thái Đạt.");
    }

    [Fact]
    public async Task CancelAcceptedAsync_WhenStudentDeleted_ReturnsBusinessError()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        student.Status = StudentStatus.Accepted;
        student.CompanyId = company.Id;
        student.JobPositionId = position.Id;
        student.IsDeleted = true; // Deleted student!

        var request = new PlacementRequest
        {
            Id = 605,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CancelAcceptedAsync(
            requestId: request.Id,
            adminUserId: adminUser.Id,
            note: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Sinh viên đã bị xóa, không thể hủy kết quả.");
    }

    [Fact]
    public async Task CancelAcceptedAsync_WhenStudentDataMismatch_ReturnsBusinessError()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        // Student's CompanyId is pointing to a different company (999)
        student.Status = StudentStatus.Accepted;
        student.CompanyId = 999;
        student.JobPositionId = position.Id;

        var request = new PlacementRequest
        {
            Id = 606,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CancelAcceptedAsync(
            requestId: request.Id,
            adminUserId: adminUser.Id,
            note: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Dữ liệu sinh viên không khớp với đơn, không thể hủy tự động.");
    }

    [Fact]
    public async Task CancelAcceptedAsync_NoteExceeds1000Chars_ReturnsBusinessError()
    {
        await using var db = CreateDbContext();
        var (company, position, student, adminUser, _) = await SeedBaseEntitiesAsync(db);

        student.Status = StudentStatus.Accepted;
        student.CompanyId = company.Id;
        student.JobPositionId = position.Id;

        var request = new PlacementRequest
        {
            Id = 607,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var longNote = new string('A', 1001);
        var service = CreateService(db);
        var result = await service.CancelAcceptedAsync(
            requestId: request.Id,
            adminUserId: adminUser.Id,
            note: longNote);

        result.Status.Should().Be(PlacementRequestOperationStatus.BusinessError);
        result.Error.Should().Be("Ghi chú không được vượt quá 1000 ký tự.");
    }

    [Fact]
    public async Task CancelAcceptedAsync_NonExistentRequest_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        await SeedBaseEntitiesAsync(db);

        var service = CreateService(db);
        var result = await service.CancelAcceptedAsync(
            requestId: 99999,
            adminUserId: 1,
            note: null);

        result.Status.Should().Be(PlacementRequestOperationStatus.NotFound);
        result.Error.Should().Be("Không tìm thấy đơn ứng tuyển.");
    }

    [Fact]
    public async Task AcceptAsync_SaveChangesThrowsDbUpdateException_ReturnsConflictWithoutPersistingChanges()
    {
        var interceptor = new ToggleFailingSaveChangesInterceptor();
        await using var db = CreateDbContext(interceptor);
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db);
        var request = new PlacementRequest
        {
            Id = 503,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        interceptor.ShouldFail = true;
        var service = CreateService(db);
        var result = await service.AcceptAsync(
            request.Id,
            company.Id,
            ActorType.Company,
            companyUser.Id,
            "Accepted",
            null);

        result.Status.Should().Be(PlacementRequestOperationStatus.Conflict);
        result.Error.Should().Be("Không thể hoàn tất, vui lòng thử lại.");

        db.ChangeTracker.Clear();
        (await db.PlacementRequests.SingleAsync(item => item.Id == request.Id))
            .Status.Should().Be(RequestStatus.Pending);
        var unchangedStudent = await db.Students.SingleAsync(item => item.Id == student.Id);
        unchangedStudent.Status.Should().Be(StudentStatus.NoCompany);
        unchangedStudent.CompanyId.Should().BeNull();
        unchangedStudent.JobPositionId.Should().BeNull();
        (await db.StatusHistories.CountAsync()).Should().Be(0);
        (await db.Notifications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AcceptAsync_NotifiesExactlyOnceOnSuccessAndDoesNotNotifyOnFailure()
    {
        await using var db = CreateDbContext();
        var (company, position, student, _, companyUser) = await SeedBaseEntitiesAsync(db);
        var request = new PlacementRequest
        {
            Id = 504,
            StudentId = student.Id,
            CompanyId = company.Id,
            JobPositionId = position.Id,
            ApplicantFullName = student.FullName,
            ApplicantEmail = student.Email,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.PlacementRequests.Add(request);
        await db.SaveChangesAsync();

        var emailSender = new CountingEmailSender();
        var emailNotifier = new PlacementRequestEmailNotifier(
            emailSender,
            NullLogger<PlacementRequestEmailNotifier>.Instance);
        var service = CreateService(db, emailNotifier);

        var success = await service.AcceptAsync(
            request.Id,
            company.Id,
            ActorType.Company,
            companyUser.Id,
            "Accepted",
            null);

        success.Status.Should().Be(PlacementRequestOperationStatus.Success);
        emailSender.SendCount.Should().Be(1);

        var failure = await service.AcceptAsync(
            requestId: 99999,
            scopeCompanyId: company.Id,
            actor: ActorType.Company,
            actorUserId: companyUser.Id,
            note: "Not found",
            confirmationSource: null);

        failure.Status.Should().Be(PlacementRequestOperationStatus.NotFound);
        emailSender.SendCount.Should().Be(1);
    }

    private sealed class ToggleFailingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public bool ShouldFail { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
            {
                throw new DbUpdateException("Injected SaveChanges failure for test.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class CountingEmailSender : IEmailSender
    {
        public int SendCount { get; private set; }

        public Task<bool> SendAsync(EmailMessageDto message, CancellationToken ct)
        {
            SendCount++;
            return Task.FromResult(true);
        }
    }
}
