using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using StudentInternshipMgmt.Application.Features.Notifications;
using StudentInternshipMgmt.Application.Features.Students;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class NotificationWorkflowTests
{
    [Fact]
    public async Task Admin_status_change_does_not_create_notification()
    {
        var seeded = await CreateSeededContext(StudentStatus.Interviewed, UserRole.Admin);
        await using var dbContext = seeded.DbContext;
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);

        var result = await service.ChangeStatusAsync(
            seeded.Student.Id,
            new ChangeStatusDto { NewStatus = StudentStatus.Introduced, Note = "Điều chỉnh hồ sơ" },
            seeded.User.Id);

        result.Success.Should().BeTrue();
        (await dbContext.Notifications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Failed_backward_change_does_not_create_notification()
    {
        var seeded = await CreateSeededContext(StudentStatus.Introduced, UserRole.Admin);
        await using var dbContext = seeded.DbContext;
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);

        var result = await service.ChangeStatusAsync(
            seeded.Student.Id,
            new ChangeStatusDto { NewStatus = StudentStatus.NoCompany },
            seeded.User.Id);

        result.Success.Should().BeFalse();
        (await dbContext.Notifications.CountAsync()).Should().Be(0);
        (await dbContext.Students.SingleAsync()).Status.Should().Be(StudentStatus.Introduced);
    }

    [Fact]
    public async Task Recent_rejection_includes_new_account_and_excludes_pending_request()
    {
        var seeded = await CreateSeededContext(StudentStatus.NoCompany);
        await using var dbContext = seeded.DbContext;
        dbContext.StatusHistories.Add(new StatusHistory
        {
            StudentId = seeded.Student.Id,
            FromStatus = StudentStatus.Introduced,
            ToStatus = StudentStatus.NoCompany,
            CompanyId = seeded.Company.Id,
            Note = "Không phù hợp",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = seeded.User.Id,
            ChangedByType = ActorType.Student
        });
        await dbContext.SaveChangesAsync();
        var dashboardService = new DashboardService(
            dbContext,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RecentRejectionDays"] = "14"
            }).Build());

        var recent = await dashboardService.GetRecentRejectionsAsync();

        recent.Should().ContainSingle();
        recent[0].StudentId.Should().Be(seeded.Student.Id);
        recent[0].RejectedCompanyName.Should().Be(seeded.Company.Name);

        dbContext.PlacementRequests.Add(new PlacementRequest
        {
            StudentId = seeded.Student.Id,
            CompanyId = seeded.Company.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        (await dashboardService.GetRecentRejectionsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Marking_notification_read_is_global_and_mark_all_records_reader()
    {
        var seeded = await CreateSeededContext(StudentStatus.NoCompany);
        await using var dbContext = seeded.DbContext;
        var notification = new Notification
        {
            Type = NotificationType.StudentReportedRejection,
            Title = "Sinh viên báo rớt",
            Message = "Nội dung",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();
        var service = new NotificationService(dbContext);

        var result = await service.MarkAsReadAsync(notification.Id, seeded.User.Id);
        var count = await service.GetUnreadCountAsync();

        result.Success.Should().BeTrue();
        count.UnreadCount.Should().Be(0);
        notification.ReadBy.Should().Be(seeded.User.Id);

        var anotherNotification = new Notification
        {
            Type = NotificationType.StudentRevertedStage,
            Title = "Sinh viên quay lại giai đoạn",
            Message = "Nội dung",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Notifications.Add(anotherNotification);
        await dbContext.SaveChangesAsync();
        await service.MarkAllAsReadAsync(seeded.User.Id);

        (await service.GetUnreadCountAsync()).UnreadCount.Should().Be(0);
        anotherNotification.ReadBy.Should().Be(seeded.User.Id);
    }

    private static async Task<SeededContext> CreateSeededContext(
        StudentStatus status,
        UserRole role = UserRole.User)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        var company = new Company
        {
            Id = 40,
            Name = "Công ty thử nghiệm",
            Address = "Địa chỉ",
            Industry = "Công nghệ",
            ContactPerson = "Người liên hệ"
        };
        var student = new Student
        {
            Id = 41,
            StudentCode = "SV0041",
            FullName = "Sinh viên thử nghiệm",
            Major = "Công nghệ",
            ClassName = "SE01",
            Email = "student@example.com",
            PhoneNumber = "0901234567",
            Status = status,
            CompanyId = company.Id,
            Company = company,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Id = 42,
            Username = "student-user",
            PasswordHash = "hash",
            Role = role,
            Student = student,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.AddRange(company, student, user);
        await dbContext.SaveChangesAsync();
        return new SeededContext(dbContext, company, student, user);
    }

    private sealed record SeededContext(AppDbContext DbContext, Company Company, Student Student, User User);
}