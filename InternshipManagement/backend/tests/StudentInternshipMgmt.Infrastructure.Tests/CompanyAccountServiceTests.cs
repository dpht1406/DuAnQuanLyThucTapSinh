using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudentInternshipMgmt.Application.Features.Auth;
using StudentInternshipMgmt.Application.Features.Auth.Dtos;
using StudentInternshipMgmt.Application.Features.Companies.Accounts;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class CompanyAccountServiceTests
{
    [Fact]
    public async Task CreateAccountAsync_creates_company_user_and_returns_temporary_credentials()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany("contact@example.com");
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).CreateAccountAsync(company.Id, adminUserId: 90);
        var user = await dbContext.Users.SingleAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CompanyId.Should().Be(company.Id);
        result.Data.Username.Should().Be($"cty{company.Id:D4}");
        result.Data.ContactEmail.Should().Be("contact@example.com");
        user.Role.Should().Be(UserRole.Company);
        user.CompanyId.Should().Be(company.Id);
        user.StudentId.Should().BeNull();
        user.MustChangePassword.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify(result.Data.TemporaryPassword, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAccountAsync_rejects_missing_contact_email_without_adding_user()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany(contactEmail: null);
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).CreateAccountAsync(company.Id, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Doanh nghiệp chưa có email liên hệ. Hãy cập nhật ContactEmail trước khi tạo tài khoản.");
        (await dbContext.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAccountAsync_rejects_invalid_contact_email()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany("not-an-email");
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).CreateAccountAsync(company.Id, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Email liên hệ của doanh nghiệp không đúng định dạng. Hãy cập nhật trước khi tạo tài khoản.");
        (await dbContext.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAccountAsync_rejects_existing_inactive_company_account()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany("contact@example.com");
        var user = CreateCompanyUser(company, "cty0001", isActive: false);
        dbContext.AddRange(company, user);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).CreateAccountAsync(company.Id, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Doanh nghiệp này đã có tài khoản.");
        (await dbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAccountAsync_rejects_username_used_by_another_user()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany("contact@example.com");
        dbContext.Companies.Add(company);
        dbContext.Users.Add(new User
        {
            Username = "cty0001",
            PasswordHash = "password-hash",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).CreateAccountAsync(company.Id, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("cty0001").And.Contain("đã được sử dụng");
        (await dbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAccountAsync_returns_not_found_for_missing_company()
    {
        await using var dbContext = CreateDbContext();

        var result = await CreateService(dbContext).CreateAccountAsync(companyId: 404, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Be("Không tìm thấy công ty.");
    }

    [Fact]
    public async Task GetAccountAsync_returns_null_for_missing_company_and_empty_details_without_account()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        (await service.GetAccountAsync(404)).Should().BeNull();

        var company = CreateCompany("contact@example.com");
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();
        var noAccount = await service.GetAccountAsync(company.Id);

        noAccount.Should().NotBeNull();
        noAccount!.HasAccount.Should().BeFalse();
        noAccount.UserId.Should().BeNull();
        noAccount.Username.Should().BeNull();
        noAccount.IsActive.Should().BeNull();
        noAccount.MustChangePassword.Should().BeNull();
        noAccount.CreatedAt.Should().BeNull();

        var user = CreateCompanyUser(company, "cty0001");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var withAccount = await service.GetAccountAsync(company.Id);

        withAccount!.HasAccount.Should().BeTrue();
        withAccount.UserId.Should().Be(user.Id);
        withAccount.Username.Should().Be(user.Username);
        withAccount.IsActive.Should().BeTrue();
        withAccount.MustChangePassword.Should().BeFalse();
        withAccount.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public async Task ResetPasswordAsync_changes_hash_revokes_tokens_and_preserves_inactive_state()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany("contact@example.com");
        var user = CreateCompanyUser(company, "cty0001", isActive: false);
        var activeTokens = new[]
        {
            CreateRefreshToken(user, "refresh-1"),
            CreateRefreshToken(user, "refresh-2")
        };
        var revokedToken = CreateRefreshToken(user, "refresh-revoked");
        revokedToken.IsRevoked = true;
        revokedToken.RevokedAt = DateTime.UtcNow.AddHours(-1);
        dbContext.AddRange(company, user);
        dbContext.RefreshTokens.AddRange(activeTokens);
        dbContext.RefreshTokens.Add(revokedToken);
        await dbContext.SaveChangesAsync();
        var oldHash = user.PasswordHash;

        var result = await CreateService(dbContext).ResetPasswordAsync(company.Id, adminUserId: 90);

        result.Success.Should().BeTrue();
        user.PasswordHash.Should().NotBe(oldHash);
        BCrypt.Net.BCrypt.Verify(result.Data!.TemporaryPassword, user.PasswordHash).Should().BeTrue();
        user.MustChangePassword.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        activeTokens.Should().OnlyContain(token => token.IsRevoked && token.RevokedAt.HasValue);
        revokedToken.RevokedAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ResetPasswordAsync_rejects_company_without_account()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany("contact@example.com");
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).ResetPasswordAsync(company.Id, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Error.Should().Be("Doanh nghiệp này chưa có tài khoản.");
    }

    [Fact]
    public async Task DeactivateAsync_revokes_tokens_and_activate_is_idempotent()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany("contact@example.com");
        var user = CreateCompanyUser(company, "cty0001");
        var token = CreateRefreshToken(user, "refresh-1");
        dbContext.AddRange(company, user, token);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var deactivateResult = await service.DeactivateAsync(company.Id, adminUserId: 90);
        var repeatedDeactivateResult = await service.DeactivateAsync(company.Id, adminUserId: 90);
        var activateResult = await service.ActivateAsync(company.Id, adminUserId: 90);
        var repeatedActivateResult = await service.ActivateAsync(company.Id, adminUserId: 90);

        deactivateResult.Success.Should().BeTrue();
        repeatedDeactivateResult.Success.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
        activateResult.Success.Should().BeTrue();
        repeatedActivateResult.Success.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Created_account_can_login_and_deactivated_account_cannot_login()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany("contact@example.com");
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();
        var accountService = CreateService(dbContext);

        var created = await accountService.CreateAccountAsync(company.Id, adminUserId: 90);
        var authService = new AuthService(dbContext, Options.Create(new JwtSettings
        {
            Issuer = "company-account-tests",
            Audience = "company-account-tests",
            SecretKey = "company-account-tests-secret-key-at-least-32-characters",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        }));
        var login = await authService.LoginAsync(new LoginRequestDto
        {
            Username = created.Data!.Username,
            Password = created.Data.TemporaryPassword
        });

        login.MustChangePassword.Should().BeTrue();
        (await accountService.DeactivateAsync(company.Id, adminUserId: 90)).Success.Should().BeTrue();
        var loginAction = () => authService.LoginAsync(new LoginRequestDto
        {
            Username = created.Data.Username,
            Password = created.Data.TemporaryPassword
        });

        await loginAction.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static CompanyAccountService CreateService(AppDbContext dbContext) =>
        new(dbContext, NullLogger<CompanyAccountService>.Instance);

    private static Company CreateCompany(string? contactEmail) => new()
    {
        Name = "Công ty thử nghiệm",
        Address = "Địa chỉ thử nghiệm",
        Industry = "Công nghệ",
        ContactPerson = "Người liên hệ",
        ContactEmail = contactEmail
    };

    private static User CreateCompanyUser(Company company, string username, bool isActive = true) => new()
    {
        Username = username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password"),
        Role = UserRole.Company,
        Company = company,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    private static RefreshToken CreateRefreshToken(User user, string token) => new()
    {
        User = user,
        Token = token,
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        CreatedAt = DateTime.UtcNow
    };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}