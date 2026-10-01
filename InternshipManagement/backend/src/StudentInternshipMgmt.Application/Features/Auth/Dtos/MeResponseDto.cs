namespace StudentInternshipMgmt.Application.Features.Auth.Dtos;

public class MeResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? StudentId { get; set; }
    public int? CompanyId { get; set; }
    public bool MustChangePassword { get; set; }
}
