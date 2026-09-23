namespace StudentInternshipMgmt.Application.Features.Auth.Dtos;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}
