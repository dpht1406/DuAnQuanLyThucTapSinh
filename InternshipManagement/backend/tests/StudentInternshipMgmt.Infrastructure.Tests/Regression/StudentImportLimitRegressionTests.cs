using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests.Regression;

public class StudentImportLimitRegressionTests
{
    [Fact]
    public async Task TC_07_1_rejects_csv_with_more_than_5000_data_rows_without_saving_students()
    {
        const int dataRowCount = 5_001;
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        using var csv = CreateCsv(dataRowCount);

        var result = await service.ImportStudentsAsync(csv, "students.csv");

        result.Errors.Should().NotBeEmpty();
        result.SuccessCount.Should().Be(0);
        (await dbContext.Students.CountAsync()).Should().Be(0);
    }

    private static MemoryStream CreateCsv(int dataRowCount)
    {
        var csv = new StringBuilder()
            .AppendLine("StudentCode,FullName,Email,PhoneNumber,Major,ClassName");

        for (var index = 1; index <= dataRowCount; index++)
        {
            csv.AppendLine($"TC{index:D5},Student {index},student{index}@example.com,0900000001,Software Engineering,SE01");
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
