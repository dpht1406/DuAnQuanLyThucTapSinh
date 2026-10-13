using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Features.Students;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class StudentSoftDeleteFilterTests
{
    [Fact]
    public async Task Students_query_excludes_soft_deleted_students()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Students.AddRange(
            new Student
            {
                StudentCode = "SV001",
                FullName = "Active Student",
                Major = "Software Engineering",
                ClassName = "SE01",
                Email = "active@example.com",
                PhoneNumber = "0900000001",
                Status = StudentStatus.NoCompany,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Student
            {
                StudentCode = "SV002",
                FullName = "Deleted Student",
                Major = "Software Engineering",
                ClassName = "SE01",
                Email = "deleted@example.com",
                PhoneNumber = "0900000002",
                Status = StudentStatus.NoCompany,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = true
            });

        await dbContext.SaveChangesAsync();

        var students = await dbContext.Students.ToListAsync();
        var allStudents = await dbContext.Students.IgnoreQueryFilters().ToListAsync();

        students.Should().ContainSingle();
        students.Single().StudentCode.Should().Be("SV001");
        allStudents.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateStudentAsync_rejects_invalid_email_and_phone_number()
    {
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);

        var invalidEmailResult = await service.CreateStudentAsync(new CreateStudentDto
        {
            StudentCode = "SV010",
            FullName = "Student Ten",
            Email = "bad-email",
            PhoneNumber = "0900000010",
            Major = "Software Engineering",
            ClassName = "SE01"
        });

        var invalidPhoneResult = await service.CreateStudentAsync(new CreateStudentDto
        {
            StudentCode = "SV011",
            FullName = "Student Eleven",
            Email = "student11@example.com",
            PhoneNumber = "123456789",
            Major = "Software Engineering",
            ClassName = "SE01"
        });

        invalidEmailResult.Success.Should().BeFalse();
        invalidEmailResult.Error.Should().Contain("Email");
        invalidPhoneResult.Success.Should().BeFalse();
        invalidPhoneResult.Error.Should().Contain("PhoneNumber");
    }

    [Fact]
    public async Task CreateStudentAsync_trims_fields_and_rejects_case_insensitive_duplicate_codes()
    {
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);

        var created = await service.CreateStudentAsync(new CreateStudentDto
        {
            StudentCode = " TST001 ",
            FullName = " Student One ",
            Email = " student1@example.com ",
            PhoneNumber = " 0900000001 ",
            Major = " Software Engineering ",
            ClassName = " SE01 "
        });
        var duplicate = await service.CreateStudentAsync(new CreateStudentDto
        {
            StudentCode = " tst001 ",
            FullName = "Student Duplicate",
            Email = "student2@example.com",
            PhoneNumber = "0900000002",
            Major = "Software Engineering",
            ClassName = "SE01"
        });

        created.Success.Should().BeTrue();
        created.Data!.StudentCode.Should().Be("TST001");
        created.Data.FullName.Should().Be("Student One");
        created.Data.Email.Should().Be("student1@example.com");
        created.Data.PhoneNumber.Should().Be("0900000001");
        created.Error.Should().BeNull();
        duplicate.Success.Should().BeFalse();
        duplicate.Error.Should().Contain("đã tồn tại");
    }

    [Fact]
    public async Task UpdateStudentAsync_trims_fields_and_rejects_invalid_email_and_phone_number()
    {
        await using var dbContext = CreateDbContext();
        var student = new Student
        {
            StudentCode = "TST012",
            FullName = "Original Name",
            Major = "Software Engineering",
            ClassName = "SE01",
            Email = "original@example.com",
            PhoneNumber = "0900000012",
            Status = StudentStatus.NoCompany,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);

        var invalidEmail = await service.UpdateStudentAsync(student.Id, new UpdateStudentDto
        {
            FullName = "Updated Name",
            Email = "bad-email",
            PhoneNumber = "0900000012",
            Major = "Software Engineering",
            ClassName = "SE01"
        });
        var invalidPhone = await service.UpdateStudentAsync(student.Id, new UpdateStudentDto
        {
            FullName = "Updated Name",
            Email = "updated@example.com",
            PhoneNumber = "123456789",
            Major = "Software Engineering",
            ClassName = "SE01"
        });
        var updated = await service.UpdateStudentAsync(student.Id, new UpdateStudentDto
        {
            FullName = " Updated Name ",
            Email = " updated@example.com ",
            PhoneNumber = " 0900000012 ",
            Major = " Software Engineering ",
            ClassName = " SE01 "
        });

        invalidEmail.Success.Should().BeFalse();
        invalidEmail.Error.Should().Contain("Email");
        invalidPhone.Success.Should().BeFalse();
        invalidPhone.Error.Should().Contain("PhoneNumber");
        updated.Success.Should().BeTrue();
        (await dbContext.Students.SingleAsync()).FullName.Should().Be("Updated Name");
        student.Email.Should().Be("updated@example.com");
        student.PhoneNumber.Should().Be("0900000012");
        student.Major.Should().Be("Software Engineering");
        student.ClassName.Should().Be("SE01");
    }

    [Fact]
    public async Task ExportStudentsAsync_applies_has_account_filter()
    {
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);

        var withAccount = new Student
        {
            StudentCode = "SV100",
            FullName = "Student With Account",
            Major = "Software Engineering",
            ClassName = "SE01",
            Email = "withaccount@example.com",
            PhoneNumber = "0900000100",
            Status = StudentStatus.NoCompany,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            User = new User
            {
                Username = "SV100",
                PasswordHash = "hash",
                Role = UserRole.User,
                MustChangePassword = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        var withoutAccount = new Student
        {
            StudentCode = "SV101",
            FullName = "Student Without Account",
            Major = "Software Engineering",
            ClassName = "SE01",
            Email = "withoutaccount@example.com",
            PhoneNumber = "0900000101",
            Status = StudentStatus.NoCompany,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await dbContext.Students.AddRangeAsync(withAccount, withoutAccount);
        await dbContext.SaveChangesAsync();

        var (content, _, _) = await service.ExportStudentsAsync(new StudentFilterDto { HasAccount = false }, "csv");
        var csv = Encoding.UTF8.GetString(content);

        csv.Should().Contain("SV101");
        csv.Should().NotContain("SV100");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
