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
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}