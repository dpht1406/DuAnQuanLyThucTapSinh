using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Application.Features.Companies.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class CompanyServiceTests
{
    [Fact]
    public async Task CreateCompanyAsync_accepts_legacy_fields_without_recruiter_details()
    {
        await using var dbContext = CreateDbContext();
        var service = new CompanyService(dbContext);

        var result = await service.CreateCompanyAsync(CreateDto());

        result.Success.Should().BeTrue();
        result.Data!.ContactPhone.Should().BeNull();
        result.Data.ContactEmail.Should().BeNull();
        result.Data.ContactPosition.Should().BeNull();
    }

    [Fact]
    public async Task CreateCompanyAsync_rejects_invalid_recruiter_phone()
    {
        await using var dbContext = CreateDbContext();
        var service = new CompanyService(dbContext);
        var dto = CreateDto();
        dto.ContactPhone = "12345";

        var result = await service.CreateCompanyAsync(dto);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Số điện thoại");
    }

    [Fact]
    public async Task CreateCompanyAsync_rejects_invalid_recruiter_email()
    {
        await using var dbContext = CreateDbContext();
        var service = new CompanyService(dbContext);
        var dto = CreateDto();
        dto.ContactEmail = "abc";

        var result = await service.CreateCompanyAsync(dto);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Email");
    }

    [Fact]
    public async Task CreateCompanyAsync_trims_valid_recruiter_details()
    {
        await using var dbContext = CreateDbContext();
        var service = new CompanyService(dbContext);
        var dto = CreateDto();
        dto.ContactPhone = " 0901234567 ";
        dto.ContactEmail = " recruiter@example.com ";
        dto.ContactPosition = " HR Manager ";

        var result = await service.CreateCompanyAsync(dto);

        result.Success.Should().BeTrue();
        result.Data!.ContactPhone.Should().Be("0901234567");
        result.Data.ContactEmail.Should().Be("recruiter@example.com");
        result.Data.ContactPosition.Should().Be("HR Manager");
    }

    [Fact]
    public async Task DeleteCompanyAsync_rejects_company_with_linked_account()
    {
        await using var dbContext = CreateDbContext();
        var company = new Company
        {
            Name = "Công ty thử nghiệm",
            Address = "Địa chỉ thử nghiệm",
            Industry = "Công nghệ",
            ContactPerson = "Người liên hệ"
        };
        var user = new User
        {
            Username = "company-user",
            PasswordHash = "password-hash",
            Role = UserRole.Company,
            Company = company,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.AddRange(company, user);
        await dbContext.SaveChangesAsync();

        var result = await new CompanyService(dbContext).DeleteCompanyAsync(company.Id);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Không thể xóa công ty vì đã có tài khoản đăng nhập liên kết với công ty này.");
        result.NotFound.Should().BeFalse();
        (await dbContext.Companies.AnyAsync(item => item.Id == company.Id)).Should().BeTrue();
    }

    private static CreateCompanyDto CreateDto() => new()
    {
        Name = "Công ty thử nghiệm",
        Address = "Địa chỉ thử nghiệm",
        Industry = "Công nghệ",
        ContactPerson = "Người liên hệ"
    };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}