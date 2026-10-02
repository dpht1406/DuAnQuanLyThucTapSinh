using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;
using Xunit.Abstractions;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class PlacementRequestServiceTests
{
    private readonly ITestOutputHelper _output;

    public PlacementRequestServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CreateRequestAsync_saves_trimmed_snapshots_without_changing_student()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedBaseAsync(dbContext);
        var originalStudent = new
        {
            fixture.Student.Id,
            fixture.Student.StudentCode,
            fixture.Student.FullName,
            fixture.Student.Major,
            fixture.Student.ClassName,
            fixture.Student.Email,
            fixture.Student.PhoneNumber,
            fixture.Student.Status,
            fixture.Student.CompanyId,
            fixture.Student.JobPositionId,
            fixture.Student.IsDeleted,
            fixture.Student.CreatedAt,
            fixture.Student.UpdatedAt
        };
        var dto = CreateValidDto(fixture.Company.Id, fixture.Position.Id);
        dto.ApplicantFullName = "  Ứng viên thử nghiệm  ";
        dto.ApplicantEmail = "  Applicant@Example.COM  ";
        dto.ApplicantPhone = " 0901234567 ";
        dto.ApplicantSchool = "  Đại học kiểm thử  ";
        dto.ApplicantMajor = "  Kỹ thuật phần mềm  ";
        dto.CvUrl = " https://example.com/cv.pdf ";
        dto.CoverLetter = "  Tôi mong muốn được tham gia thực tập.  ";

        var result = await new PlacementRequestService(dbContext, NullLogger<PlacementRequestService>.Instance).CreateRequestAsync(fixture.Student.Id, dto);

        result.Success.Should().BeTrue();
        var request = await dbContext.PlacementRequests.SingleAsync();
        request.ApplicantFullName.Should().Be("Ứng viên thử nghiệm");
        request.ApplicantEmail.Should().Be("applicant@example.com");
        request.ApplicantPhone.Should().Be("0901234567");
        request.ApplicantSchool.Should().Be("Đại học kiểm thử");
        request.ApplicantMajor.Should().Be("Kỹ thuật phần mềm");
        request.CvUrl.Should().Be("https://example.com/cv.pdf");
        request.CoverLetter.Should().Be("Tôi mong muốn được tham gia thực tập.");
        new
        {
            fixture.Student.Id,
            fixture.Student.StudentCode,
            fixture.Student.FullName,
            fixture.Student.Major,
            fixture.Student.ClassName,
            fixture.Student.Email,
            fixture.Student.PhoneNumber,
            fixture.Student.Status,
            fixture.Student.CompanyId,
            fixture.Student.JobPositionId,
            fixture.Student.IsDeleted,
            fixture.Student.CreatedAt,
            fixture.Student.UpdatedAt
        }.Should().BeEquivalentTo(originalStudent);
    }

    [Fact]
    public async Task CreateRequestAsync_uses_form_email_without_changing_profile_email()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedBaseAsync(dbContext);
        var dto = CreateValidDto(fixture.Company.Id, fixture.Position.Id);
        dto.ApplicantEmail = "new.contact@example.com";

        var result = await new PlacementRequestService(dbContext, NullLogger<PlacementRequestService>.Instance).CreateRequestAsync(fixture.Student.Id, dto);

        result.Success.Should().BeTrue();
        (await dbContext.PlacementRequests.SingleAsync()).ApplicantEmail.Should().Be("new.contact@example.com");
        fixture.Student.Email.Should().Be("profile@example.com");
    }

    [Theory]
    [InlineData("closed", "Vị trí này đã ngừng tuyển.")]
    [InlineData("expired", "Vị trí này đã hết hạn nhận hồ sơ.")]
    [InlineData("other-company", "Vị trí tuyển dụng không thuộc doanh nghiệp đã chọn.")]
    [InlineData("missing-position", "Không tìm thấy vị trí tuyển dụng.")]
    public async Task CreateRequestAsync_rejects_unavailable_or_mismatched_position(string scenario, string expectedError)
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedBaseAsync(dbContext);
        var dto = CreateValidDto(fixture.Company.Id, fixture.Position.Id);

        switch (scenario)
        {
            case "closed": fixture.Position.IsOpen = false; break;
            case "expired": fixture.Position.Deadline = DateTime.Today.AddDays(-1); break;
            case "other-company": dto.CompanyId = fixture.OtherCompany.Id; break;
            case "missing-position": dto.JobPositionId = 999; break;
            default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        var result = await new PlacementRequestService(dbContext, NullLogger<PlacementRequestService>.Instance).CreateRequestAsync(fixture.Student.Id, dto);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(expectedError);
        (await dbContext.PlacementRequests.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateRequestAsync_rejects_student_with_pending_request()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedBaseAsync(dbContext);
        dbContext.PlacementRequests.Add(new PlacementRequest
        {
            StudentId = fixture.Student.Id,
            Student = fixture.Student,
            CompanyId = fixture.Company.Id,
            Company = fixture.Company,
            JobPositionId = fixture.Position.Id,
            JobPosition = fixture.Position,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var result = await new PlacementRequestService(dbContext, NullLogger<PlacementRequestService>.Instance).CreateRequestAsync(
            fixture.Student.Id,
            CreateValidDto(fixture.Company.Id, fixture.Position.Id));

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Bạn đã có một yêu cầu đang chờ duyệt.");
        (await dbContext.PlacementRequests.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateRequestAsync_rejects_student_not_in_no_company_status()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedBaseAsync(dbContext);
        fixture.Student.Status = StudentStatus.Introduced;
        await dbContext.SaveChangesAsync();

        var result = await new PlacementRequestService(dbContext, NullLogger<PlacementRequestService>.Instance).CreateRequestAsync(
            fixture.Student.Id,
            CreateValidDto(fixture.Company.Id, fixture.Position.Id));

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Chỉ có thể tạo yêu cầu khi chưa có doanh nghiệp.");
    }

    [Fact]
    public async Task GetMyRequestsAsync_reads_legacy_request_without_snapshot_fields()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedBaseAsync(dbContext);
        dbContext.PlacementRequests.Add(new PlacementRequest
        {
            StudentId = fixture.Student.Id,
            Student = fixture.Student,
            CompanyId = fixture.Company.Id,
            Company = fixture.Company,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var result = await new PlacementRequestService(dbContext, NullLogger<PlacementRequestService>.Instance).GetMyRequestsAsync(fixture.Student.Id);

        result.Should().ContainSingle();
        result[0].ApplicantFullName.Should().BeNull();
        result[0].ApplicantEmail.Should().BeNull();
        result[0].ApplicantPhone.Should().BeNull();
        result[0].ApplicantSchool.Should().BeNull();
        result[0].ApplicantMajor.Should().BeNull();
        result[0].CvUrl.Should().BeNull();
        result[0].CoverLetter.Should().BeNull();
    }

    [Fact]
    public async Task CreateRequestAsync_logs_safe_fields_for_success_and_rejection()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedBaseAsync(dbContext);
        var logger = new CapturingLogger();
        var service = new PlacementRequestService(dbContext, logger);
        var dto = CreateValidDto(fixture.Company.Id, fixture.Position.Id);
        dto.ApplicantEmail = "private.applicant@example.com";
        dto.ApplicantPhone = "0912345678";
        dto.CvUrl = "https://example.com/private-cv";
        dto.CoverLetter = "Thông tin riêng tư của ứng viên trong đơn.";

        var created = await service.CreateRequestAsync(fixture.Student.Id, dto);
        var rejected = await service.CreateRequestAsync(fixture.Student.Id, dto);

        created.Success.Should().BeTrue();
        rejected.Success.Should().BeFalse();
        var informationLine = logger.Entries.Single(entry => entry.Level == LogLevel.Information).Message;
        var warningLine = logger.Entries.Single(entry => entry.Level == LogLevel.Warning).Message;
        informationLine.Should().Be("Placement request created: RequestId 1, StudentId 1, CompanyId 10, JobPositionId 100");
        warningLine.Should().Be("Placement request rejected: PendingRequestExists; StudentId 1, CompanyId 10, JobPositionId 100");
        new[] { informationLine, warningLine }.Should().NotContain(message =>
            message.Contains(dto.ApplicantEmail, StringComparison.Ordinal)
            || message.Contains(dto.ApplicantPhone, StringComparison.Ordinal)
            || message.Contains(dto.CvUrl, StringComparison.Ordinal)
            || message.Contains(dto.CoverLetter, StringComparison.Ordinal));
        _output.WriteLine($"Information: {informationLine}");
        _output.WriteLine($"Warning: {warningLine}");
    }

    private static CreatePlacementRequestDto CreateValidDto(int companyId, int jobPositionId) => new()
    {
        CompanyId = companyId,
        JobPositionId = jobPositionId,
        ApplicantFullName = "Ứng viên thử nghiệm",
        ApplicantEmail = "applicant@example.com",
        ApplicantPhone = "0901234567",
        ApplicantSchool = "Đại học thử nghiệm",
        ApplicantMajor = "Công nghệ thông tin",
        CvUrl = "https://example.com/cv.pdf",
        CoverLetter = "Tôi mong muốn được tham gia thực tập."
    };

    private static async Task<TestFixture> SeedBaseAsync(AppDbContext dbContext)
    {
        var company = CreateCompany(10);
        var otherCompany = CreateCompany(20);
        var position = CreatePosition(100, company);
        var student = new Student
        {
            Id = 1,
            StudentCode = "SV0001",
            FullName = "Sinh viên trong hồ sơ",
            Major = "Ngành trong hồ sơ",
            ClassName = "SE01",
            Email = "profile@example.com",
            PhoneNumber = "0911111111",
            Status = StudentStatus.NoCompany,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.AddRange(company, otherCompany, position, student);
        await dbContext.SaveChangesAsync();
        return new TestFixture(company, otherCompany, position, student);
    }

    private static Company CreateCompany(int id) => new()
    {
        Id = id,
        Name = $"Công ty {id}",
        Address = "Địa chỉ thử nghiệm",
        Industry = "Công nghệ",
        ContactPerson = "Người liên hệ"
    };

    private static JobPosition CreatePosition(int id, Company company) => new()
    {
        Id = id,
        CompanyId = company.Id,
        Company = company,
        Title = "Vị trí thực tập",
        Department = "Công nghệ",
        Quantity = 1,
        Description = "Mô tả vị trí",
        IsOpen = true
    };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed record TestFixture(Company Company, Company OtherCompany, JobPosition Position, Student Student);

    private sealed class CapturingLogger : ILogger<PlacementRequestService>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}