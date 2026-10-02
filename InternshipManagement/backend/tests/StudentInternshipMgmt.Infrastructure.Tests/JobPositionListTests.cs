using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Features.JobPositions.Dtos;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class JobPositionListTests
{
    [Fact]
    public async Task GetJobPositionByIdAsync_WithoutLocation_UsesCompanyAddress()
    {
        await using var dbContext = CreateDbContext();
        await AddCompanyAndPositionAsync(dbContext, CreatePosition(1, 1));
        var service = new JobPositionService(dbContext);

        var result = await service.GetJobPositionByIdAsync(1);

        result!.DisplayLocation.Should().Be("Địa chỉ công ty");
        result.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetJobPositionByIdAsync_WithLocation_UsesPositionLocation()
    {
        await using var dbContext = CreateDbContext();
        var position = CreatePosition(1, 1);
        position.Location = "Văn phòng Quận 3";
        await AddCompanyAndPositionAsync(dbContext, position);
        var service = new JobPositionService(dbContext);

        var result = await service.GetJobPositionByIdAsync(1);

        result!.DisplayLocation.Should().Be("Văn phòng Quận 3");
        result.Location.Should().Be("Văn phòng Quận 3");
    }

    [Fact]
    public async Task GetJobPositionsAsync_WithPastTodayAndNullDeadlines_SetsExpirationByDate()
    {
        await using var dbContext = CreateDbContext();
        await AddCompanyAndPositionsAsync(dbContext,
            CreatePosition(1, 1, DateTime.Today.AddDays(-1)),
            CreatePosition(2, 1, DateTime.Today),
            CreatePosition(3, 1, null));
        var service = new JobPositionService(dbContext);

        var result = await service.GetJobPositionsAsync(new JobPositionFilterDto { PageSize = 20 });

        result.Items.Single(position => position.Id == 1).IsExpired.Should().BeTrue();
        result.Items.Single(position => position.Id == 2).IsExpired.Should().BeFalse();
        result.Items.Single(position => position.Id == 3).IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task CreateJobPositionAsync_WithPastDeadline_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var service = new JobPositionService(dbContext);

        var result = await service.CreateJobPositionAsync(new CreateJobPositionDto
        {
            CompanyId = 1,
            Title = "Vị trí thử nghiệm",
            Department = "Phòng Công nghệ",
            Deadline = DateTime.Today.AddDays(-1),
            Quantity = 1
        });

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Hạn nộp hồ sơ không được trước ngày hôm nay.");
    }

    [Fact]
    public async Task CreateJobPositionAsync_WithDepartmentOver100Characters_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var service = new JobPositionService(dbContext);

        var result = await service.CreateJobPositionAsync(new CreateJobPositionDto
        {
            CompanyId = 1,
            Title = "Vị trí thử nghiệm",
            Department = new string('A', 101),
            Quantity = 1
        });

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Phòng ban không được quá 100 ký tự.");
    }

    [Fact]
    public async Task GetJobPositionsAsync_WithAvailability_ReturnsMatchingOpenAndClosedPositions()
    {
        await using var dbContext = CreateDbContext();
        await AddCompanyAndPositionsAsync(dbContext,
            CreatePosition(1, 1, DateTime.Today),
            CreatePosition(2, 1, DateTime.Today.AddDays(-1)),
            CreatePosition(3, 1, null, isOpen: false));
        var service = new JobPositionService(dbContext);

        var open = await service.GetJobPositionsAsync(new JobPositionFilterDto { Availability = "open", PageSize = 20 });
        var closed = await service.GetJobPositionsAsync(new JobPositionFilterDto { Availability = "closed", PageSize = 20 });

        open.Items.Select(position => position.Id).Should().Equal(1);
        closed.Items.Select(position => position.Id).Should().BeEquivalentTo(new[] { 2, 3 });
    }

    [Fact]
    public async Task GetJobPositionsAsync_SearchesCompanyNameAndDepartment()
    {
        await using var dbContext = CreateDbContext();
        await AddCompanyAndPositionsAsync(dbContext,
            CreatePosition(1, 1, companyName: "Công ty Mặt Trời", department: "Phòng Kỹ thuật"),
            CreatePosition(2, 2, companyName: "Doanh nghiệp Khác", department: "Phòng Dữ liệu"));
        var service = new JobPositionService(dbContext);

        var companyMatches = await service.GetJobPositionsAsync(new JobPositionFilterDto { Search = "mặt trời", PageSize = 20 });
        var departmentMatches = await service.GetJobPositionsAsync(new JobPositionFilterDto { Search = "dữ liệu", PageSize = 20 });

        companyMatches.Items.Select(position => position.Id).Should().Equal(1);
        departmentMatches.Items.Select(position => position.Id).Should().Equal(2);
    }

    [Fact]
    public async Task GetJobPositionsAsync_WithRecommendedSort_PrioritizesAvailableAndEarlierDeadlines()
    {
        await using var dbContext = CreateDbContext();
        await AddCompanyAndPositionsAsync(dbContext,
            CreatePosition(1, 1, DateTime.Today.AddDays(5)),
            CreatePosition(2, 1, null),
            CreatePosition(3, 1, DateTime.Today.AddDays(-1)),
            CreatePosition(4, 1, DateTime.Today.AddDays(1), isOpen: false));
        var service = new JobPositionService(dbContext);

        var result = await service.GetJobPositionsAsync(new JobPositionFilterDto { Sort = "recommended", PageSize = 20 });

        result.Items.Select(position => position.Id).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task CreateRequestAsync_WithExpiredPosition_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany(1, "Công ty thử nghiệm");
        var position = CreatePosition(1, 1, DateTime.Today.AddDays(-1));
        position.Company = company;
        var student = new Student
        {
            Id = 1,
            StudentCode = "SV001",
            FullName = "Sinh viên thử nghiệm",
            Major = "Công nghệ thông tin",
            ClassName = "CT01",
            Email = "student@example.com",
            PhoneNumber = "0900000000",
            Status = StudentStatus.NoCompany,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Companies.Add(company);
        dbContext.JobPositions.Add(position);
        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync();
        var service = new PlacementRequestService(dbContext, NullLogger<PlacementRequestService>.Instance);

        var result = await service.CreateRequestAsync(1, new CreatePlacementRequestDto { CompanyId = 1, JobPositionId = 1 });

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Vị trí này đã hết hạn nhận hồ sơ.");
    }

    [Fact]
    public async Task GetJobPositionsAsync_ForUser_DoesNotExposeAcceptedCount()
    {
        await using var dbContext = CreateDbContext();
        await AddCompanyAndPositionAsync(dbContext, CreatePosition(1, 1));
        var service = new JobPositionService(dbContext);

        var result = await service.GetJobPositionsAsync(new JobPositionFilterDto());
        var serialized = JsonSerializer.Serialize(result.Items);

        serialized.Should().NotContain("AcceptedCount");
        result.Items.Should().ContainSingle();
    }

    private static JobPosition CreatePosition(int id, int companyId, DateTime? deadline = null, bool isOpen = true, string? companyName = null, string department = "Phòng Công nghệ") => new()
    {
        Id = id,
        CompanyId = companyId,
        Company = CreateCompany(companyId, companyName ?? "Công ty thử nghiệm"),
        Title = $"Vị trí {id}",
        Department = department,
        Deadline = deadline,
        Quantity = 1,
        Description = "Mô tả thử nghiệm",
        IsOpen = isOpen
    };

    private static Company CreateCompany(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Address = "Địa chỉ công ty",
        Industry = "Công nghệ",
        ContactPerson = "Người liên hệ"
    };

    private static async Task AddCompanyAndPositionAsync(AppDbContext dbContext, JobPosition position)
    {
        await AddCompanyAndPositionsAsync(dbContext, position);
    }

    private static async Task AddCompanyAndPositionsAsync(AppDbContext dbContext, params JobPosition[] positions)
    {
        var companies = positions
            .Select(position => position.Company)
            .GroupBy(company => company.Id)
            .Select(group => group.First())
            .ToDictionary(company => company.Id);
        foreach (var position in positions)
            position.Company = companies[position.CompanyId];

        dbContext.Companies.AddRange(companies.Values);
        dbContext.JobPositions.AddRange(positions);
        await dbContext.SaveChangesAsync();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}