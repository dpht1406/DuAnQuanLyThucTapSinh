using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentInternshipMgmt.Application.Features.Companies.Accounts;
using StudentInternshipMgmt.Application.Features.Companies.Dtos;
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

    public static string GenerateUsername(int companyId) => "cty" + companyId.ToString("D4");

    public static (bool IsValid, string? Error, string? NormalizedEmail) ValidateContactEmail(string? rawEmail)
    {
        if (string.IsNullOrWhiteSpace(rawEmail))
            return (false, "Doanh nghiệp chưa có email liên hệ. Hãy cập nhật ContactEmail trước khi tạo tài khoản.", null);

        var email = rawEmail.Trim();
        if (!ContactEmailRegex.IsMatch(email))
            return (false, "Email liên hệ của doanh nghiệp không đúng định dạng. Hãy cập nhật trước khi tạo tài khoản.", null);

        return (true, null, email);
    }

    public static User CreateCompanyUserEntity(int companyId, string username, string temporaryPassword)
    {
        return new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            Role = UserRole.Company,
            CompanyId = companyId,
            StudentId = null,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };
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

        var (emailValid, emailError, contactEmail) = ValidateContactEmail(company.ContactEmail);
        if (!emailValid)
            return (false, emailError, false, null);

        var existingAccount = await _db.Users.AnyAsync(user =>
            user.CompanyId == companyId && user.Role == UserRole.Company);
        if (existingAccount)
            return (false, "Doanh nghiệp này đã có tài khoản.", false, null);

        var username = GenerateUsername(companyId);
        var usernameInUse = await _db.Users.AnyAsync(user => user.Username == username);
        if (usernameInUse)
            return (false, $"Tên đăng nhập '{username}' đã được sử dụng.", false, null);

        var temporaryPassword = PasswordGenerator.Generate();
        var user = CreateCompanyUserEntity(companyId, username, temporaryPassword);
        _db.Users.Add(user);

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
            ContactEmail = contactEmail!
        });
    }

    public async Task<CreateCompanyAccountsResultDto> CreateAccountsBulkAsync(
        List<int> companyIds,
        int adminUserId)
    {
        _logger.LogInformation(
            "Admin {AdminUserId} requested bulk company account creation for {Count} company IDs",
            adminUserId,
            companyIds?.Count ?? 0);

        var result = new CreateCompanyAccountsResultDto();
        if (companyIds == null || companyIds.Count == 0)
            return result;

        var distinctIds = companyIds.Distinct().ToList();

        var companies = await _db.Companies
            .Include(c => c.User)
            .Where(c => distinctIds.Contains(c.Id))
            .ToListAsync();

        var companyMap = companies.ToDictionary(c => c.Id);

        // Preload existing candidate usernames to eliminate N+1 queries in the loop
        var candidateUsernames = distinctIds.Select(GenerateUsername).ToList();
        var existingUsernames = (await _db.Users
            .Where(u => candidateUsernames.Contains(u.Username))
            .Select(u => u.Username)
            .ToListAsync()).ToHashSet();

        foreach (var companyId in distinctIds)
        {
            if (!companyMap.TryGetValue(companyId, out var company))
            {
                result.SkippedCount++;
                result.Skipped.Add(new SkippedCompanyAccountDto
                {
                    CompanyId = companyId,
                    CompanyName = string.Empty,
                    Reason = "Không tìm thấy doanh nghiệp."
                });
                continue;
            }

            // 1. Kiểm tra doanh nghiệp đã có tài khoản chưa (đã nạp qua Include)
            var hasAccount = company.User != null && company.User.Role == UserRole.Company;
            if (hasAccount)
            {
                result.SkippedCount++;
                result.Skipped.Add(new SkippedCompanyAccountDto
                {
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    Reason = "Doanh nghiệp này đã có tài khoản."
                });
                continue;
            }

            // 2. Kiểm tra email liên hệ hợp lệ
            var (emailValid, emailError, contactEmail) = ValidateContactEmail(company.ContactEmail);
            if (!emailValid)
            {
                result.SkippedCount++;
                result.Skipped.Add(new SkippedCompanyAccountDto
                {
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    Reason = emailError!
                });
                continue;
            }

            // 3. Kiểm tra tên đăng nhập đã được dùng chưa (tra cứu in-memory O(1))
            var username = GenerateUsername(company.Id);
            if (existingUsernames.Contains(username))
            {
                result.SkippedCount++;
                result.Skipped.Add(new SkippedCompanyAccountDto
                {
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    Reason = $"Tên đăng nhập '{username}' đã được sử dụng."
                });
                continue;
            }

            // 4. Tạo tài khoản
            var temporaryPassword = PasswordGenerator.Generate();
            var user = CreateCompanyUserEntity(company.Id, username, temporaryPassword);
            _db.Users.Add(user);

            try
            {
                await _db.SaveChangesAsync();
                existingUsernames.Add(username);
                result.SuccessCount++;
                result.Created.Add(new CreatedCompanyAccountDto
                {
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    Username = username,
                    TemporaryPassword = temporaryPassword,
                    ContactEmail = contactEmail!
                });
            }
            catch (DbUpdateException)
            {
                _db.Entry(user).State = EntityState.Detached;
                result.SkippedCount++;
                result.Skipped.Add(new SkippedCompanyAccountDto
                {
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    Reason = "Doanh nghiệp này đã có tài khoản."
                });
            }
        }

        return result;
    }

    public async Task<(bool Success, string? Error, CreateCompanyAccountsResultDto? Data)> CreateAccountsBulkByFilterAsync(
        CompanyFilterDto filter,
        int adminUserId)
    {
        var query = _db.Companies.AsQueryable();
        query = CompanyService.ApplyCompanyFilter(query, filter);

        var totalMatches = await query.CountAsync();
        if (totalMatches > 500)
        {
            return (false,
                "Số lượng doanh nghiệp khớp bộ lọc vượt quá giới hạn (tối đa 500). Vui lòng thu hẹp bộ lọc.",
                null);
        }

        var companyIds = await query.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();
        var result = await CreateAccountsBulkAsync(companyIds, adminUserId);
        return (true, null, result);
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
