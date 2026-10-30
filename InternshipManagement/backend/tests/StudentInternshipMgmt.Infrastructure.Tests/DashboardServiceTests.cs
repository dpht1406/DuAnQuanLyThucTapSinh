using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_returns_zero_completion_rate_when_there_are_no_students()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var summary = await service.GetSummaryAsync();

        summary.TotalStudents.Should().Be(0);
        summary.CompletionRate.Should().Be(0);
    }

    [Fact]
    public async Task GetSummaryAsync_rounds_completion_rate_to_two_decimal_places()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Students.AddRange(
            CreateStudent(1, StudentStatus.Completed),
            CreateStudent(2, StudentStatus.NoCompany),
            CreateStudent(3, StudentStatus.NoCompany));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var summary = await service.GetSummaryAsync();

        summary.TotalStudents.Should().Be(3);
        summary.CompletedCount.Should().Be(1);
        summary.CompletionRate.Should().Be(33.33);
    }

    [Fact]
    public async Task GetSummaryAsync_excludes_soft_deleted_completed_students()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Students.AddRange(
            CreateStudent(1, StudentStatus.Completed),
            CreateStudent(2, StudentStatus.NoCompany),
            CreateStudent(3, StudentStatus.NoCompany),
            CreateStudent(4, StudentStatus.Completed, isDeleted: true));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var summary = await service.GetSummaryAsync();

        summary.TotalStudents.Should().Be(3);
        summary.CompletedCount.Should().Be(1);
        summary.CompletionRate.Should().Be(33.33);
    }

    [Fact]
    public async Task GetOverdueRequestsAsync_returns_pending_requests_due_within_alert_window()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTime.UtcNow;
        var student = CreateStudent(1, StudentStatus.NoCompany);
        var company = new Company
        {
            Id = 1,
            Name = "Company 1",
            Address = "Address",
            Industry = "Technology",
            ContactPerson = "Contact"
        };
        dbContext.AddRange(
            student,
            company,
            new PlacementRequest
            {
                Id = 1,
                StudentId = student.Id,
                CompanyId = company.Id,
                Status = RequestStatus.Pending,
                CreatedAt = now.AddDays(-30),
                DueAt = now.AddDays(3)
            },
            new PlacementRequest
            {
                Id = 2,
                StudentId = student.Id,
                CompanyId = company.Id,
                Status = RequestStatus.Pending,
                CreatedAt = now,
                DueAt = now.AddDays(-2)
            },
            new PlacementRequest
            {
                Id = 3,
                StudentId = student.Id,
                CompanyId = company.Id,
                Status = RequestStatus.Pending,
                CreatedAt = now,
                DueAt = now.AddDays(6)
            },
            new PlacementRequest
            {
                Id = 4,
                StudentId = student.Id,
                CompanyId = company.Id,
                Status = RequestStatus.Pending,
                CreatedAt = now,
                DueAt = null
            },
            new PlacementRequest
            {
                Id = 5,
                StudentId = student.Id,
                CompanyId = company.Id,
                Status = RequestStatus.InterviewInvited,
                CreatedAt = now,
                DueAt = now.AddDays(1)
            });
        await dbContext.SaveChangesAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OverdueRequestAlertDays"] = "5"
            })
            .Build();
        var service = new DashboardService(dbContext, configuration);

        var result = await service.GetOverdueRequestsAsync();

        result.Select(request => request.Id).Should().Equal(2, 1);
    }

    private static DashboardService CreateService(AppDbContext dbContext)
    {
        var configuration = new ConfigurationBuilder().Build();
        return new DashboardService(dbContext, configuration);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Student CreateStudent(int id, StudentStatus status, bool isDeleted = false) => new()
    {
        Id = id,
        StudentCode = $"SV{id:D3}",
        FullName = $"Student {id}",
        Major = "Software Engineering",
        ClassName = "SE01",
        Email = $"student{id}@example.com",
        PhoneNumber = "0900000001",
        Status = status,
        IsDeleted = isDeleted,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
