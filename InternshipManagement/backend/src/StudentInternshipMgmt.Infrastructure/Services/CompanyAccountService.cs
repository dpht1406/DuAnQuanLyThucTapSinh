using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentInternshipMgmt.Application.Features.Companies.Accounts;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class CompanyAccountService : ICompanyAccountService
{
    private const string CompanyNotFoundError = "Không tìm thấy công ty.";
    private const string AccountNotFoundError = "Doanh nghiệp này chưa có tài khoản.";

    private static readonly Regex ContactEmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly ILogger<CompanyAccountService> _logger;

    public CompanyAccountService(AppDbContext db, ILogger<CompanyAccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CompanyAccountDto?> GetAccountAsync(int companyId)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);
        if (company is null)
            return null;

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(item => item.CompanyId == companyId && item.Role == UserRole.Company);

        if (user is null)
            return new CompanyAccountDto { HasAccount = false };

        return new CompanyAccountDto
        {
            HasAccount = true,
            UserId = user.Id,
            Username = user.Username,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<(bool Success, string? Error, bool NotFound, CompanyAccountCredentialsDto? Data)> CreateAccountAsync(
        int companyId,
        int adminUserId)
    {
        _logger.LogInformation(
            "Admin {AdminUserId} requested company account creation for company {CompanyId}",
            adminUserId,
            companyId);

        var company = await _db.Companies.FirstOrDefaultAsync(item => item.Id == companyId);
        if (company is null)
            return (false, CompanyNotFoundError, true, null);

        if (string.IsNullOrWhiteSpace(company.ContactEmail))
            return (false,
                "Doanh nghiệp chưa có email liên hệ. Hãy cập nhật ContactEmail trước khi tạo tài khoản.",
                false,
                null);

        var contactEmail = company.ContactEmail.Trim();
        if (!ContactEmailRegex.IsMatch(contactEmail))
            return (false,
                "Email liên hệ của doanh nghiệp không đúng định dạng. Hãy cập nhật trước khi tạo tài khoản.",
                false,
                null);

        var existingAccount = await _db.Users.AnyAsync(user =>
            user.CompanyId == companyId && user.Role == UserRole.Company);
        if (existingAccount)
            return (false, "Doanh nghiệp này đã có tài khoản.", false, null);

        var username = "cty" + companyId.ToString("D4");
        var usernameInUse = await _db.Users.AnyAsync(user => user.Username == username);
        if (usernameInUse)
            return (false, $"Tên đăng nhập '{username}' đã được sử dụng.", false, null);

        var temporaryPassword = PasswordGenerator.Generate();
        _db.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            Role = UserRole.Company,
            CompanyId = companyId,
            StudentId = null,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Doanh nghiệp này đã có tài khoản.", false, null);
        }

        return (true, null, false, new CompanyAccountCredentialsDto
        {
            CompanyId = companyId,
            Username = username,
            TemporaryPassword = temporaryPassword,
            ContactEmail = contactEmail
        });
    }

    public async Task<(bool Success, string? Error, bool NotFound, CompanyAccountCredentialsDto? Data)> ResetPasswordAsync(
        int companyId,
        int adminUserId)
    {
        _logger.LogInformation(
            "Admin {AdminUserId} requested company account password reset for company {CompanyId}",
            adminUserId,
            companyId);

        var company = await _db.Companies.FirstOrDefaultAsync(item => item.Id == companyId);
        if (company is null)
            return (false, CompanyNotFoundError, true, null);

        var user = await _db.Users.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId && item.Role == UserRole.Company);
        if (user is null)
            return (false, AccountNotFoundError, false, null);

        var temporaryPassword = PasswordGenerator.Generate();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        user.MustChangePassword = true;
        await RevokeActiveRefreshTokensAsync(user.Id);

        await _db.SaveChangesAsync();
        return (true, null, false, new CompanyAccountCredentialsDto
        {
            CompanyId = companyId,
            Username = user.Username,
            TemporaryPassword = temporaryPassword,
            ContactEmail = company.ContactEmail ?? string.Empty
        });
    }

    public async Task<(bool Success, string? Error, bool NotFound)> DeactivateAsync(
        int companyId,
        int adminUserId)
    {
        _logger.LogInformation(
            "Admin {AdminUserId} requested company account deactivation for company {CompanyId}",
            adminUserId,
            companyId);

        var companyExists = await _db.Companies.AnyAsync(item => item.Id == companyId);
        if (!companyExists)
            return (false, CompanyNotFoundError, true);

        var user = await _db.Users.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId && item.Role == UserRole.Company);
        if (user is null)
            return (false, AccountNotFoundError, false);

        user.IsActive = false;
        await RevokeActiveRefreshTokensAsync(user.Id);
        await _db.SaveChangesAsync();
        return (true, null, false);
    }

    public async Task<(bool Success, string? Error, bool NotFound)> ActivateAsync(
        int companyId,
        int adminUserId)
    {
        _logger.LogInformation(
            "Admin {AdminUserId} requested company account activation for company {CompanyId}",
            adminUserId,
            companyId);

        var companyExists = await _db.Companies.AnyAsync(item => item.Id == companyId);
        if (!companyExists)
            return (false, CompanyNotFoundError, true);

        var user = await _db.Users.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId && item.Role == UserRole.Company);
        if (user is null)
            return (false, AccountNotFoundError, false);

        user.IsActive = true;
        await _db.SaveChangesAsync();
        return (true, null, false);
    }

    private async Task RevokeActiveRefreshTokensAsync(int userId)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(token => token.UserId == userId && !token.IsRevoked)
            .ToListAsync();
        var revokedAt = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = revokedAt;
        }
    }
}