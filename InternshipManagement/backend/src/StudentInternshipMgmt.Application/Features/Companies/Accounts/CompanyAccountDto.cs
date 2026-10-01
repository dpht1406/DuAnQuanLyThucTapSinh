namespace StudentInternshipMgmt.Application.Features.Companies.Accounts;

public class CompanyAccountDto
{
    public bool HasAccount { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public bool? IsActive { get; set; }
    public bool? MustChangePassword { get; set; }
    public DateTime? CreatedAt { get; set; }
}