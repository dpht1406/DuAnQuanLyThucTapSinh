using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Companies.Accounts;

public class CreateCompanyAccountsRequestDto
{
    public List<int> CompanyIds { get; set; } = new();
}

public class CreatedCompanyAccountDto
{
    public int CompanyId { get; set; }

    [SafeString(200)]
    public string CompanyName { get; set; } = string.Empty;

    [SafeString(50)]
    public string Username { get; set; } = string.Empty;

    // The plain-text password exists ONLY in this response and client-side export request.
    // It is NEVER persisted to the database.
    [SafeString(300)]
    public string TemporaryPassword { get; set; } = string.Empty;

    [SafeString(150)]
    public string ContactEmail { get; set; } = string.Empty;
}

public class SkippedCompanyAccountDto
{
    public int CompanyId { get; set; }

    [SafeString(200)]
    public string CompanyName { get; set; } = string.Empty;

    [SafeString(300)]
    public string Reason { get; set; } = string.Empty;
}

public class CreateCompanyAccountsResultDto
{
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public List<CreatedCompanyAccountDto> Created { get; set; } = new();
    public List<SkippedCompanyAccountDto> Skipped { get; set; } = new();
}

public class ExportCompanyAccountItemDto
{
    [SafeString(200)]
    public string CompanyName { get; set; } = string.Empty;

    [SafeString(50)]
    public string Username { get; set; } = string.Empty;

    [SafeString(300)]
    public string TemporaryPassword { get; set; } = string.Empty;

    [SafeString(150)]
    public string ContactEmail { get; set; } = string.Empty;
}

public class ExportCompanyAccountsRequestDto
{
    public List<ExportCompanyAccountItemDto> Accounts { get; set; } = new();
}
