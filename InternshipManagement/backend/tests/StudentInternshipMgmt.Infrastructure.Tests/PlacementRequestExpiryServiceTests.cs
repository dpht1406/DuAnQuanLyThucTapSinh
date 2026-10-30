using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class PlacementRequestExpiryServiceTests
{
    [Fact]
    public async Task ExpireOverdueAsync_expires_only_overdue_pending_requests_and_is_idempotent()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);
        var student = CreateStudent();
        var company = CreateCompany();
        var overduePending = CreateRequest(1, student, company, RequestStatus.Pending, now.AddTicks(-1));
        overduePending.ReviewedBy = 42;
        overduePending.ReviewedAt = now.AddDays(-1);
        var dueNowPending = CreateRequest(2, student, company, RequestStatus.Pending, now);
        var futurePending = CreateRequest(3, student, company, RequestStatus.Pending, now.AddTicks(1));
        var overdueInterview = CreateRequest(4, student, company, RequestStatus.InterviewInvited, now.AddDays(-1));
        var noDueDatePending = CreateRequest(5, student, company, RequestStatus.Pending, dueAt: null);
        var overdueAccepted = CreateRequest(6, student, company, RequestStatus.Accepted, now.AddDays(-1));
        overdueAccepted.ResultNote = "Accepted result";
        overdueAccepted.ResultActorType = ActorType.Company;
        var overdueRejected = CreateRequest(7, student, company, RequestStatus.Rejected, now.AddDays(-1));
        overdueRejected.ResultNote = "Rejected result";
        overdueRejected.ResultActorType = ActorType.Company;
        dbContext.AddRange(student, company, overduePending, dueNowPending, futurePending,
            overdueInterview, noDueDatePending, overdueAccepted, overdueRejected);
        await dbContext.SaveChangesAsync();
        var service = new PlacementRequestExpiryService(
            dbContext,
            NullLogger<PlacementRequestExpiryService>.Instance);

        var expiredCount = await service.ExpireOverdueAsync(now, CancellationToken.None);
        var secondExpiredCount = await service.ExpireOverdueAsync(now, CancellationToken.None);

        expiredCount.Should().Be(1);
        secondExpiredCount.Should().Be(0);
        overduePending.Status.Should().Be(RequestStatus.Expired);
        overduePending.ResultActorType.Should().Be(ActorType.System);
        overduePending.ResultNote.Should().Be("Doanh nghiệp không phản hồi trước hạn");
        overduePending.ReviewedAt.Should().Be(now);
        overduePending.ReviewedBy.Should().BeNull();
        dueNowPending.Status.Should().Be(RequestStatus.Pending);
        futurePending.Status.Should().Be(RequestStatus.Pending);
        overdueInterview.Status.Should().Be(RequestStatus.InterviewInvited);
        noDueDatePending.Status.Should().Be(RequestStatus.Pending);
        overdueAccepted.Status.Should().Be(RequestStatus.Accepted);
        overdueAccepted.ResultNote.Should().Be("Accepted result");
        overdueAccepted.ResultActorType.Should().Be(ActorType.Company);
        overdueRejected.Status.Should().Be(RequestStatus.Rejected);
        overdueRejected.ResultNote.Should().Be("Rejected result");
        overdueRejected.ResultActorType.Should().Be(ActorType.Company);
    }

    [Fact]
    public async Task ExpireOverdueAsync_processes_requests_in_multiple_batches()
    {
        await using var dbContext = CreateDbContext();
        var student = CreateStudent();
        var company = CreateCompany();
        var dueAt = DateTime.UtcNow.AddDays(-1);
        var requests = Enumerable.Range(1, 205)
            .Select(id => CreateRequest(id, student, company, RequestStatus.Pending, dueAt))
            .ToList();
        dbContext.AddRange(student, company);
        dbContext.PlacementRequests.AddRange(requests);
        await dbContext.SaveChangesAsync();
        var service = new PlacementRequestExpiryService(
            dbContext,
            NullLogger<PlacementRequestExpiryService>.Instance);

        var expiredCount = await service.ExpireOverdueAsync(DateTime.UtcNow, CancellationToken.None);

        expiredCount.Should().Be(205);
        requests.Should().OnlyContain(request => request.Status == RequestStatus.Expired);
    }

    private static PlacementRequest CreateRequest(
        int id,
        Student student,
        Company company,
        RequestStatus status,
        DateTime? dueAt) => new()
    {
        Id = id,
        StudentId = student.Id,
        Student = student,
        CompanyId = company.Id,
        Company = company,
        Status = status,
        CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        DueAt = dueAt
    };

    private static Student CreateStudent() => new()
    {
        Id = 1,
        StudentCode = "SV001",
        FullName = "Student",
        Major = "Software Engineering",
        ClassName = "SE01",
        Email = "student@example.com",
        PhoneNumber = "0900000001",
        Status = StudentStatus.NoCompany,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Company CreateCompany() => new()
    {
        Id = 1,
        Name = "Company",
        Address = "Address",
        Industry = "Technology",
        ContactPerson = "Contact"
    };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
