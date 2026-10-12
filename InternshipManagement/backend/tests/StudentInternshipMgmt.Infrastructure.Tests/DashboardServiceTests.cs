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
