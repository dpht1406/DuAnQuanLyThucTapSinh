using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests.Regression;

public class StudentImportRegressionTests
{
    [Fact]
    public async Task TC_05_1_rejects_popular_gmail_typos_and_invalid_local_part()
    {
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        using var csv = CreateCsv("a@gmial.com", "a@gmail.con", "a#b@gmail.com");

        var result = await service.ImportStudentsAsync(csv, "students.csv");

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().HaveCount(3);
        result.Errors.Select(error => error.Reason).Should().Equal(
            "Email có vẻ gõ sai. Ý bạn là a@gmail.com?",
            "Email có vẻ gõ sai. Ý bạn là a@gmail.com?",
            EmailAddressValidator.GetErrorMessage("a#b@gmail.com"));
        (await dbContext.Students.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("a@gmail.com")]
    [InlineData("first.last+tag@outlook.com")]
    [InlineData("a@school.edu.vn")]
    [InlineData("user_name@custom-domain.org")]
    [InlineData("x@y.co")]
    [InlineData("a@gmial.com")]
    [InlineData("a@gmail.con")]
    [InlineData("a#b@gmail.com")]
    [InlineData("a..b@gmail.com")]
    [InlineData("a@localhost")]
    public async Task TC_05_2_import_email_validation_matches_shared_validator(string email)
    {
        var expectedValid = EmailAddressValidator.IsValid(email);
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        using var csv = CreateCsv(email);

        var result = await service.ImportStudentsAsync(csv, "students.csv");

        if (expectedValid)
        {
            result.Errors.Should().BeEmpty();
            result.SuccessCount.Should().Be(1);
        }
        else
        {
            result.SuccessCount.Should().Be(0);
            result.Errors.Should().ContainSingle()
                .Which.Reason.Should().Be(EmailAddressValidator.GetErrorMessage(email));
        }
    }

    private static MemoryStream CreateCsv(params string[] emails)
    {
        var csv = new StringBuilder()
            .AppendLine("StudentCode,FullName,Email,PhoneNumber,Major,ClassName");

        for (var index = 0; index < emails.Length; index++)
        {
            csv.AppendLine($"TC{index + 1:D5},Student {index + 1},{emails[index]},0900000001,Software Engineering,SE01");
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
