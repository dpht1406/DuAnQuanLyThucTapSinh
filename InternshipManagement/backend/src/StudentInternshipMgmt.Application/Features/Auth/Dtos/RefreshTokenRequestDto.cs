using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Auth.Dtos;

public class RefreshTokenRequestDto
{
    [SafeString(512)]
    public string RefreshToken { get; set; } = string.Empty;
}
