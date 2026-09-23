using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;

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

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
