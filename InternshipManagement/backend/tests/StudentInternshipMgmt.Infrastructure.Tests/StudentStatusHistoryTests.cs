using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Features.Students;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class StudentStatusHistoryTests
{
    [Fact]
    public async Task ChangeStatusAsync_preserves_company_in_history_when_student_reports_rejection()
    {
        await using var dbContext = CreateDbContext();
        var company = new Company
        {
            Id = 40,
            Name = "Công ty vừa rớt",
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
            Status = StudentStatus.Introduced,
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
            Role = UserRole.User,
            Student = student,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.AddRange(company, student, user);
        await dbContext.SaveChangesAsync();

        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        var result = await service.ChangeStatusAsync(
            student.Id,
            new ChangeStatusDto { NewStatus = StudentStatus.NoCompany, Note = "Không phù hợp" },
            user.Id,
            isAdmin: false);
        var history = await service.GetStatusHistoryAsync(student.Id);

        result.Success.Should().BeTrue();
        history.Should().ContainSingle();
        history![0].CompanyId.Should().Be(company.Id);
        history[0].CompanyName.Should().Be(company.Name);
        history[0].Note.Should().Be("Không phù hợp");
        (await dbContext.StatusHistories.SingleAsync()).ChangedByType.Should().Be(ActorType.Student);
    }

    [Fact]
    public async Task Request_approval_records_admin_actor_type()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany(50);
        var student = CreateStudent(51, company.Id, StudentStatus.NoCompany);
        var request = new PlacementRequest
        {
            Id = 52,
            StudentId = student.Id,
            Student = student,
            CompanyId = company.Id,
            Company = company,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.AddRange(company, student, request);
        await dbContext.SaveChangesAsync();

        var service = new PlacementRequestService(dbContext, NullLogger<PlacementRequestService>.Instance);
        var result = await service.ApproveRequestAsync(request.Id, adminUserId: 53);

        result.Success.Should().BeTrue();
        (await dbContext.StatusHistories.SingleAsync()).ChangedByType.Should().Be(ActorType.Admin);
    }

    [Fact]
    public async Task Admin_status_change_records_admin_actor_type()
    {
        await using var dbContext = CreateDbContext();
        var student = CreateStudent(61, companyId: 60, StudentStatus.Introduced);
        var admin = CreateAdmin(62);
        dbContext.AddRange(CreateCompany(60), student, admin);
        await dbContext.SaveChangesAsync();

        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        var result = await service.ChangeStatusAsync(
            student.Id,
            new ChangeStatusDto { NewStatus = StudentStatus.Interviewed },
            admin.Id,
            isAdmin: true);

        result.Success.Should().BeTrue();
        (await dbContext.StatusHistories.SingleAsync()).ChangedByType.Should().Be(ActorType.Admin);
    }

    [Fact]
    public async Task Direct_company_assignment_records_admin_actor_type()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany(70);
        var student = CreateStudent(71, companyId: null, StudentStatus.NoCompany);
        var admin = CreateAdmin(72);
        dbContext.AddRange(company, student, admin);
        await dbContext.SaveChangesAsync();

        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        var result = await service.AssignCompanyDirectAsync(
            student.Id,
            new AssignCompanyDto { CompanyId = company.Id },
            admin.Id);

        result.Success.Should().BeTrue();
        (await dbContext.StatusHistories.SingleAsync()).ChangedByType.Should().Be(ActorType.Admin);
    }

    [Fact]
    public void Existing_request_status_numeric_values_are_unchanged()
    {
        ((int)RequestStatus.Pending).Should().Be(0);
        ((int)RequestStatus.Approved).Should().Be(1);
        ((int)RequestStatus.Rejected).Should().Be(2);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Company CreateCompany(int id) => new()
    {
        Id = id,
        Name = $"Company {id}",
        Address = "Address",
        Industry = "Technology",
        ContactPerson = "Contact"
    };

    private static Student CreateStudent(int id, int? companyId, StudentStatus status) => new()
    {
        Id = id,
        StudentCode = $"SV{id}",
        FullName = $"Student {id}",
        Major = "Technology",
        ClassName = "SE01",
        Email = $"student{id}@example.com",
        PhoneNumber = "0901234567",
        Status = status,
        CompanyId = companyId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static User CreateAdmin(int id) => new()
    {
        Id = id,
        Username = $"admin{id}",
        PasswordHash = "hash",
        Role = UserRole.Admin,
        CreatedAt = DateTime.UtcNow
    };
}