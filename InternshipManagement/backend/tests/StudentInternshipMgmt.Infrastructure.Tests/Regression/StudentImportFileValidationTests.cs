using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests.Regression;

public class StudentImportFileValidationTests
{
    [Fact]
    public async Task ImportStudentsAsync_rejects_unsupported_file_extension()
    {
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not a spreadsheet"));

        var result = await service.ImportStudentsAsync(stream, "students.txt");

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle()
            .Which.Reason.Should().Contain("Định dạng file không hợp lệ");
    }

    [Fact]
    public async Task ImportStudentsAsync_reports_csv_missing_required_columns_as_unreadable()
    {
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("StudentCode,FullName\nSV001,Student"));

        var result = await service.ImportStudentsAsync(stream, "students.csv");

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle()
            .Which.Reason.Should().StartWith("Không đọc được file:");
    }

    [Fact]
    public async Task ImportStudentsAsync_imports_valid_rows_and_reports_mixed_file_errors_by_row()
    {
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        await using var stream = File.OpenRead(FindMixedErrorsFile());

        var result = await service.ImportStudentsAsync(stream, "students-mixed-errors.csv");

        result.SuccessCount.Should().Be(5);
        result.Errors.Select(error => error.RowNumber).Should().Equal(3, 4, 5, 7);
    }

    private static string FindMixedErrorsFile()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var path = Path.Combine(
                current.FullName,
                "docs",
                "test-quan-ly-sinh-vien",
                "evidence",
                "data",
                "students-mixed-errors.csv");
            if (File.Exists(path))
            {
                return path;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not find students-mixed-errors.csv.");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
