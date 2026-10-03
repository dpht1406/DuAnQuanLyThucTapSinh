using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Auth.Dtos;

public class LoginRequestDto
{
    [SafeString(100)]
    public string Username { get; set; } = string.Empty;
    [SafeString(300)]
    public string Password { get; set; } = string.Empty;
}
