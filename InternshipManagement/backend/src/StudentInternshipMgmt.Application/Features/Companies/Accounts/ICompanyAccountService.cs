namespace StudentInternshipMgmt.Application.Features.Companies.Accounts;

public interface ICompanyAccountService
{
    Task<CompanyAccountDto?> GetAccountAsync(int companyId);

    Task<(bool Success, string? Error, bool NotFound, CompanyAccountCredentialsDto? Data)> CreateAccountAsync(
        int companyId,
        int adminUserId);

    Task<(bool Success, string? Error, bool NotFound, CompanyAccountCredentialsDto? Data)> ResetPasswordAsync(
        int companyId,
        int adminUserId);

    Task<(bool Success, string? Error, bool NotFound)> DeactivateAsync(int companyId, int adminUserId);

    Task<(bool Success, string? Error, bool NotFound)> ActivateAsync(int companyId, int adminUserId);
}