using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;
using Xunit.Abstractions;

namespace StudentInternshipMgmt.Infrastructure.Tests.Regression;

public class StudentImportPerformanceMeasurementTests
{
    private readonly ITestOutputHelper _output;

    public StudentImportPerformanceMeasurementTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Measure_sample_csv_imports_with_in_memory_database_three_times_each()
    {
        var dataDirectory = FindRepositoryRoot();
        var sampleFiles = new[]
        {
            ("students-large-1000.csv", 1_000),
            ("students-large-5000.csv", 5_000)
        };

        _output.WriteLine("Measurement level: service/InMemory; one fresh database per run.");
        foreach (var (fileName, expectedCount) in sampleFiles)
        {
            var filePath = Path.Combine(dataDirectory, fileName);
            for (var run = 1; run <= 3; run++)
            {
                await using var dbContext = CreateDbContext();
                var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
                await using var stream = File.OpenRead(filePath);
                var stopwatch = Stopwatch.StartNew();

                var result = await service.ImportStudentsAsync(stream, fileName);

                stopwatch.Stop();
                result.Errors.Should().BeEmpty();
                result.SuccessCount.Should().Be(expectedCount);
                _output.WriteLine($"{fileName},{run},{stopwatch.Elapsed.TotalMilliseconds:F2}");
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var dataDirectory = Path.Combine(
                current.FullName,
                "docs",
                "test-quan-ly-sinh-vien",
                "evidence",
                "data");
            if (File.Exists(Path.Combine(dataDirectory, "students-large-1000.csv")) &&
                File.Exists(Path.Combine(dataDirectory, "students-large-5000.csv")))
            {
                return dataDirectory;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the student import performance sample CSV files.");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
