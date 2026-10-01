namespace StudentInternshipMgmt.Application.Features.Companies.Accounts;

public class CompanyAccountCredentialsDto
{
    public int CompanyId { get; set; }
    public string Username { get; set; } = string.Empty;

    // The plain-text password exists only in this response.
    public string TemporaryPassword { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;
}