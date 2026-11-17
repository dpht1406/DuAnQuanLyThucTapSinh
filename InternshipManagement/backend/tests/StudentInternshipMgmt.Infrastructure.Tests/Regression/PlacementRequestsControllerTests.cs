using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Api.Controllers;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests.Regression;

public class PlacementRequestsControllerTests
{
    [Fact]
    public async Task GetRequestById_returns_the_full_admin_list_dto()
    {
        await using var dbContext = CreateDbContext();
        var student = new Student
        {
            Id = 1,
            StudentCode = "SV0001",
            FullName = "Sinh viên kiểm thử",
            Major = "Công nghệ thông tin",
            ClassName = "SE01",
            Email = "student@example.com",
            PhoneNumber = "0901234567",
            Status = StudentStatus.NoCompany,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var company = new Company
        {
            Id = 2,
            Name = "Công ty kiểm thử",
            Address = "Địa chỉ",
            Industry = "Công nghệ",
            ContactPerson = "Người liên hệ"
        };
        var position = new JobPosition
        {
            Id = 3,
            CompanyId = company.Id,
            Company = company,
            Title = "Thực tập sinh",
            Department = "Kỹ thuật",
            Quantity = 1,
            Description = "Mô tả",
            IsOpen = true
        };
        var dismissedAt = DateTime.UtcNow;
        var request = new PlacementRequest
        {
            Id = 42,
            StudentId = student.Id,
            Student = student,
            CompanyId = company.Id,
            Company = company,
            JobPositionId = position.Id,
            JobPosition = position,
            Status = RequestStatus.InterviewInvited,
            StudentReportDismissedAt = dismissedAt,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.AddRange(student, company, position, request);
        await dbContext.SaveChangesAsync();
        var service = new PlacementRequestService(
            dbContext,
            NullLogger<PlacementRequestService>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection().Build());
        var controller = new PlacementRequestsController(service);

        var action = await controller.GetRequestById(request.Id);

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Which;
        var response = ok.Value.Should().BeOfType<ApiResponse<PlacementRequestDto>>().Which;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.StudentFullName.Should().Be(student.FullName);
        response.Data.CompanyName.Should().Be(company.Name);
        response.Data.JobPositionTitle.Should().Be(position.Title);
        response.Data.StudentReportDismissedAt.Should().Be(dismissedAt);
    }

    [Fact]
    public async Task GetRequestById_returns_api_failure_response_when_request_does_not_exist()
    {
        await using var dbContext = CreateDbContext();
        var service = new PlacementRequestService(
            dbContext,
            NullLogger<PlacementRequestService>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection().Build());
        var controller = new PlacementRequestsController(service);

        var action = await controller.GetRequestById(42);

        var notFound = action.Result.Should().BeOfType<NotFoundObjectResult>().Which;
        var response = notFound.Value.Should().BeOfType<ApiResponse<PlacementRequestDto>>().Which;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Không tìm thấy đơn ứng tuyển.");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
