using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Api.Controllers;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Companies.Accounts;
using StudentInternshipMgmt.Application.Features.Companies.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;
using Xunit;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class CompanyAccountsBulkTests
{
    [Fact]
    public async Task CreateAccountsBulkAsync_creates_multiple_company_accounts_successfully()
    {
        await using var db = CreateDbContext();
        var c1 = CreateCompany("c1@example.com", "Cong ty 1");
        var c2 = CreateCompany("c2@example.com", "Cong ty 2");
        var c3 = CreateCompany("c3@example.com", "Cong ty 3");
        db.Companies.AddRange(c1, c2, c3);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateAccountsBulkAsync(new List<int> { c1.Id, c2.Id, c3.Id }, adminUserId: 1);

        result.SuccessCount.Should().Be(3);
        result.SkippedCount.Should().Be(0);
        result.Created.Should().HaveCount(3);
        result.Skipped.Should().BeEmpty();

        var users = await db.Users.OrderBy(u => u.CompanyId).ToListAsync();
        users.Should().HaveCount(3);

        foreach (var created in result.Created)
        {
            var user = users.Single(u => u.CompanyId == created.CompanyId);
            user.Username.Should().Be($"cty{created.CompanyId:D4}");
            user.Role.Should().Be(UserRole.Company);
            user.IsActive.Should().BeTrue();
            user.MustChangePassword.Should().BeTrue();
            BCrypt.Net.BCrypt.Verify(created.TemporaryPassword, user.PasswordHash).Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateAccountsBulkAsync_passwords_are_stored_as_bcrypt_hashes_not_plain_text()
    {
        await using var db = CreateDbContext();
        var company = CreateCompany("secure@example.com", "Cong ty Secure");
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateAccountsBulkAsync(new List<int> { company.Id }, adminUserId: 1);

        result.SuccessCount.Should().Be(1);
        var created = result.Created.Single();
        var user = await db.Users.SingleAsync(u => u.CompanyId == company.Id);

        // Mật khẩu lưu trong DB là chuỗi băm BCrypt, tuyệt đối không phải plain-text
        user.PasswordHash.Should().NotBe(created.TemporaryPassword);
        user.PasswordHash.Should().StartWith("$2");
        BCrypt.Net.BCrypt.Verify(created.TemporaryPassword, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAccountsBulkAsync_skips_companies_with_individual_reasons_and_does_not_break_valid_ones()
    {
        await using var db = CreateDbContext();

        // 1. Đã có tài khoản
        var cHasAccount = CreateCompany("has-account@example.com", "Cong ty Da Co");
        db.Companies.Add(cHasAccount);
        await db.SaveChangesAsync();
        db.Users.Add(new User
        {
            Username = "cty" + cHasAccount.Id.ToString("D4"),
            PasswordHash = "hash",
            Role = UserRole.Company,
            CompanyId = cHasAccount.Id,
            CreatedAt = DateTime.UtcNow
        });

        // 2. Thiếu email
        var cNoEmail = CreateCompany(null, "Cong ty Khong Email");
        // 3. Email sai định dạng
        var cInvalidEmail = CreateCompany("invalid-email-string", "Cong ty Email Sai");
        // 4. Trùng username do tài khoản khác chiếm
        var cDupUser = CreateCompany("dup-user@example.com", "Cong ty Trung Username");
        db.Companies.AddRange(cNoEmail, cInvalidEmail, cDupUser);
        await db.SaveChangesAsync();

        // User khác chiếm sẵn username của cDupUser
        db.Users.Add(new User
        {
            Username = "cty" + cDupUser.Id.ToString("D4"),
            PasswordHash = "hash",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        });

        // 5. Công ty hợp lệ
        var cValid = CreateCompany("valid@example.com", "Cong ty Hop Le");
        db.Companies.Add(cValid);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var ids = new List<int>
        {
            cHasAccount.Id,
            cNoEmail.Id,
            cInvalidEmail.Id,
            cDupUser.Id,
            9999, // Không tồn tại
            cValid.Id
        };

        var result = await service.CreateAccountsBulkAsync(ids, adminUserId: 1);

        result.SuccessCount.Should().Be(1);
        result.SkippedCount.Should().Be(5);
        result.Created.Single().CompanyId.Should().Be(cValid.Id);

        result.Skipped.Should().Contain(s => s.CompanyId == cHasAccount.Id && s.Reason == "Doanh nghiệp này đã có tài khoản.");
        result.Skipped.Should().Contain(s => s.CompanyId == cNoEmail.Id && s.Reason == "Doanh nghiệp chưa có email liên hệ. Hãy cập nhật ContactEmail trước khi tạo tài khoản.");
        result.Skipped.Should().Contain(s => s.CompanyId == cInvalidEmail.Id && s.Reason == "Email liên hệ của doanh nghiệp không đúng định dạng. Hãy cập nhật trước khi tạo tài khoản.");
        result.Skipped.Should().Contain(s => s.CompanyId == cDupUser.Id && s.Reason.Contains("đã được sử dụng"));
        result.Skipped.Should().Contain(s => s.CompanyId == 9999 && s.Reason == "Không tìm thấy doanh nghiệp.");
    }

    [Fact]
    public async Task CreateAccountsBulkAsync_deduplicates_duplicate_company_ids()
    {
        await using var db = CreateDbContext();
        var c1 = CreateCompany("dup1@example.com", "Cong ty Dup 1");
        var c2 = CreateCompany("dup2@example.com", "Cong ty Dup 2");
        db.Companies.AddRange(c1, c2);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateAccountsBulkAsync(new List<int> { c1.Id, c1.Id, c2.Id, c2.Id }, adminUserId: 1);

        result.SuccessCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
        result.Created.Should().HaveCount(2);
        (await db.Users.CountAsync(u => u.Role == UserRole.Company)).Should().Be(2);
    }

    [Fact]
    public async Task CreateAccountsBulkAsync_running_twice_skips_all_on_second_run()
    {
        await using var db = CreateDbContext();
        var c1 = CreateCompany("twice1@example.com", "Cong ty Twice 1");
        var c2 = CreateCompany("twice2@example.com", "Cong ty Twice 2");
        db.Companies.AddRange(c1, c2);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var firstRun = await service.CreateAccountsBulkAsync(new List<int> { c1.Id, c2.Id }, adminUserId: 1);
        firstRun.SuccessCount.Should().Be(2);
        firstRun.SkippedCount.Should().Be(0);

        var secondRun = await service.CreateAccountsBulkAsync(new List<int> { c1.Id, c2.Id }, adminUserId: 1);
        secondRun.SuccessCount.Should().Be(0);
        secondRun.SkippedCount.Should().Be(2);
        secondRun.Skipped.Should().AllSatisfy(s => s.Reason.Should().Be("Doanh nghiệp này đã có tài khoản."));
    }

    [Fact]
    public async Task CompanyFilter_HasAccount_filters_correctly()
    {
        await using var db = CreateDbContext();
        var cWithAccount = CreateCompany("with@example.com", "Cong ty Co TK");
        var cWithoutAccount = CreateCompany("without@example.com", "Cong ty Khong TK");
        db.Companies.AddRange(cWithAccount, cWithoutAccount);
        await db.SaveChangesAsync();

        db.Users.Add(new User
        {
            Username = "cty" + cWithAccount.Id.ToString("D4"),
            PasswordHash = "hash",
            Role = UserRole.Company,
            CompanyId = cWithAccount.Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var query = db.Companies.Include(c => c.User).AsQueryable();

        // 1. HasAccount = true
        var filterTrue = new CompanyFilterDto { HasAccount = true };
        var resultTrue = await CompanyService.ApplyCompanyFilter(query, filterTrue).ToListAsync();
        resultTrue.Should().ContainSingle(c => c.Id == cWithAccount.Id);

        // 2. HasAccount = false
        var filterFalse = new CompanyFilterDto { HasAccount = false };
        var resultFalse = await CompanyService.ApplyCompanyFilter(query, filterFalse).ToListAsync();
        resultFalse.Should().ContainSingle(c => c.Id == cWithoutAccount.Id);

        // 3. HasAccount = null (tất cả)
        var filterNull = new CompanyFilterDto { HasAccount = null };
        var resultAll = await CompanyService.ApplyCompanyFilter(query, filterNull).ToListAsync();
        resultAll.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAccountsBulkByFilterAsync_rejects_more_than_500_companies()
    {
        await using var db = CreateDbContext();
        var companies = Enumerable.Range(1, 501)
            .Select(i => new Company
            {
                Name = $"Cong ty {i}",
                Address = "Dia chi",
                Industry = "IT",
                ContactPerson = "Nguoi lien he",
                ContactEmail = $"c{i}@example.com"
            });
        db.Companies.AddRange(companies);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (success, error, data) = await service.CreateAccountsBulkByFilterAsync(new CompanyFilterDto(), adminUserId: 1);

        success.Should().BeFalse();
        error.Should().Contain("500");
        data.Should().BeNull();
    }

    [Fact]
    public void ExportCsv_escapes_formula_injection_and_includes_utf8_bom()
    {
        var controller = new CompanyAccountsBulkController(null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var request = new ExportCompanyAccountsRequestDto
        {
            Accounts = new List<ExportCompanyAccountItemDto>
            {
                new()
                {
                    CompanyName = "=cmd|' /C calc'!A0",
                    Username = "+cty0001",
                    TemporaryPassword = "@secretFormula",
                    ContactEmail = "-admin@example.com"
                }
            }
        };

        var result = controller.ExportAccounts(request) as FileContentResult;

        result.Should().NotBeNull();
        result!.ContentType.Should().Be("text/csv");
        result.FileDownloadName.Should().StartWith("company_accounts_").And.EndWith(".csv");

        var preamble = Encoding.UTF8.GetPreamble();
        result.FileContents.Take(preamble.Length).Should().Equal(preamble);

        var content = Encoding.UTF8.GetString(result.FileContents);
        content.Should().Contain("Username,TemporaryPassword,CompanyName,ContactEmail");
        // Kiểm tra SpreadsheetSafety escape các ký tự formula nguy hiểm (=, +, -, @) thành ('...)
        content.Should().Contain("'+cty0001");
        content.Should().Contain("'@secretFormula");
        content.Should().Contain("'=cmd|' /C calc'!A0");
        content.Should().Contain("'-admin@example.com");
    }

    [Fact]
    public async Task Controller_rejects_empty_or_over_500_ids()
    {
        var controller = new CompanyAccountsBulkController(null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // 1. Rỗng
        var emptyRes = await controller.CreateAccountsBulk(new CreateCompanyAccountsRequestDto { CompanyIds = new() });
        (emptyRes.Result as BadRequestObjectResult).Should().NotBeNull();

        // 2. Quá 500
        var over500Res = await controller.CreateAccountsBulk(new CreateCompanyAccountsRequestDto
        {
            CompanyIds = Enumerable.Range(1, 501).ToList()
        });
        (over500Res.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    private static CompanyAccountService CreateService(AppDbContext db) =>
        new(db, NullLogger<CompanyAccountService>.Instance);

    private static Company CreateCompany(string? contactEmail, string name = "Công ty test") => new()
    {
        Name = name,
        Address = "Địa chỉ test",
        Industry = "Công nghệ thông tin",
        ContactPerson = "Người liên hệ",
        ContactEmail = contactEmail
    };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
