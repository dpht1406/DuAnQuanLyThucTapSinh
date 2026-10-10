// Chạy bằng cách đặt STUDENT_TEST_SQLSERVER trỏ tới một DB test riêng, không phải DB dev.
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Students;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class StudentSearchTests
{
    [StudentSqlServerFact]
    public async Task Search_uses_one_field_and_matches_accents_email_student_code_and_literal_like_characters()
    {
        var connectionString = Environment.GetEnvironmentVariable("STUDENT_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "STUDENT_TEST_SQLSERVER must point to a dedicated SQL Server test database.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var token = Guid.NewGuid().ToString("N");
        var codeSuffix = Random.Shared.Next(100000, 999999).ToString();
        var specialToken = new string(Enumerable.Range(0, 12)
            .Select(_ => (char)('a' + Random.Shared.Next(26)))
            .ToArray());
        var accentedName = CreateStudent($"TEST-{codeSuffix}-01", $"Phát Nguyễn {specialToken}", $"phat-{token}@example.test");
        var emailOnly = CreateStudent($"TEST-{codeSuffix}-02", "Email-only Person", $"phat-only-{token}@example.test");
        var codeMatch = CreateStudent($"MSSV-{codeSuffix}-{token[..8]}", "Mã hồ sơ", $"code-{token}@example.test");
        var percentName = CreateStudent($"TEST-{codeSuffix}-03", $"Symbol{specialToken}% Person", $"percent-{token}@example.test");
        var percentDecoy = CreateStudent($"TEST-{codeSuffix}-04", $"Symbol{specialToken}X Person", $"percent-decoy-{token}@example.test");
        var underscoreName = CreateStudent($"TEST-{codeSuffix}-05", $"Symbol{specialToken}_ Person", $"underscore-{token}@example.test");
        var underscoreDecoy = CreateStudent($"TEST-{codeSuffix}-06", $"Symbol{specialToken}X Person", $"underscore-decoy-{token}@example.test");
        var dStrokeName = CreateStudent($"TEST-{codeSuffix}-07", $"Đung {specialToken}", $"dung-{token}@example.test");
        var bracketName = CreateStudent($"TEST-{codeSuffix}-08", $"Symbol{specialToken}[ Person", $"bracket-{token}@example.test");
        var bracketDecoy = CreateStudent($"TEST-{codeSuffix}-09", $"Symbol{specialToken}X Person", $"bracket-decoy-{token}@example.test");
        dbContext.Students.AddRange(
            accentedName, emailOnly, codeMatch, percentName, percentDecoy, underscoreName, underscoreDecoy,
            dStrokeName, bracketName, bracketDecoy);
        await dbContext.SaveChangesAsync();

        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);

        var unaccentedResult = await Search(service, $" pHaT {specialToken} ");
        unaccentedResult.Items.Should().Contain(student => student.Id == accentedName.Id);
        unaccentedResult.Items.Should().NotContain(student => student.Id == emailOnly.Id);

        var unaccentedSurnameResult = await Search(service, $"NGUYEN {specialToken}");
        unaccentedSurnameResult.Items.Should().Contain(student => student.Id == accentedName.Id);

        var fullEmailResult = await Search(service, $"phat-only-{token}@example.test");
        fullEmailResult.Items.Select(student => student.Id).Should().ContainSingle().Which.Should().Be(emailOnly.Id);

        var partialEmailResult = await Search(service, $"phat-only-{token}@example");
        partialEmailResult.Items.Select(student => student.Id).Should().ContainSingle().Which.Should().Be(emailOnly.Id);

        var fullCodeResult = await Search(service, codeMatch.StudentCode);
        fullCodeResult.Items.Select(student => student.Id).Should().ContainSingle().Which.Should().Be(codeMatch.Id);

        var partialCodeResult = await Search(service, $"{codeSuffix[1..]}-{token[..8]}");
        partialCodeResult.Items.Should().Contain(student => student.Id == codeMatch.Id);

        var percentResult = await Search(service, $"{specialToken}%");
        percentResult.Items.Select(student => student.Id).Should().ContainSingle().Which.Should().Be(percentName.Id);
        percentResult.Items.Should().NotContain(student => student.Id == percentDecoy.Id);

        var underscoreResult = await Search(service, $"{specialToken}_");
        underscoreResult.Items.Select(student => student.Id).Should().ContainSingle().Which.Should().Be(underscoreName.Id);
        underscoreResult.Items.Should().NotContain(student => student.Id == underscoreDecoy.Id);

        var bracketResult = await Search(service, $"{specialToken}[");
        bracketResult.Items.Select(student => student.Id).Should().ContainSingle().Which.Should().Be(bracketName.Id);
        bracketResult.Items.Should().NotContain(student => student.Id == bracketDecoy.Id);

        var dStrokeResult = await Search(service, $"dung {specialToken}");
        dStrokeResult.Items.Should().NotContain(student => student.Id == dStrokeName.Id);

        var noMatchResult = await Search(service, $"missing-{token}");
        noMatchResult.Items.Should().BeEmpty();
        noMatchResult.TotalCount.Should().Be(0);

        var export = await service.ExportStudentsAsync(
            new StudentFilterDto { Search = $"nguyen {specialToken}" },
            "csv");
        var exportCsv = System.Text.Encoding.UTF8.GetString(export.Content);
        exportCsv.Should().Contain($"Phát Nguyễn {specialToken}");
        exportCsv.Should().NotContain("Email-only Person");
    }

    private static async Task<PagedResult<StudentDto>> Search(
        StudentService service,
        string search)
    {
        return await service.GetStudentsAsync(new StudentFilterDto
        {
            Search = search
        });
    }

    private static Student CreateStudent(string studentCode, string fullName, string email)
    {
        var now = DateTime.UtcNow;
        return new Student
        {
            StudentCode = studentCode,
            FullName = fullName,
            Major = "Test",
            ClassName = "TEST",
            Email = email,
            PhoneNumber = "0900000000",
            Status = StudentStatus.NoCompany,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

public sealed class StudentSqlServerFactAttribute : FactAttribute
{
    public StudentSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STUDENT_TEST_SQLSERVER")))
        {
            Skip = "Set STUDENT_TEST_SQLSERVER to a dedicated SQL Server test database to run this test.";
        }
    }
}
