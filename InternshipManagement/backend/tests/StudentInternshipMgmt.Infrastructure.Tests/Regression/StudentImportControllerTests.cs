using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudentInternshipMgmt.API.Controllers;
using StudentInternshipMgmt.Application.Features.Students;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests.Regression;

public class StudentImportControllerTests
{
    [Fact]
    public async Task ImportStudents_returns_413_when_file_exceeds_configured_maximum()
    {
        await using var dbContext = CreateDbContext();
        var service = new StudentService(dbContext, NullLogger<StudentService>.Instance);
        var controller = new StudentsController(
            service,
            Options.Create(new ImportSettings { MaxFileSizeBytes = 2 }));
        var stream = new MemoryStream(new byte[3]);
        var file = new FormFile(stream, 0, stream.Length, "file", "students.csv");

        var action = await controller.ImportStudents(file);

        action.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
