using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Auth.Dtos;

public class ChangePasswordRequestDto
{
    [SafeString(300)]
    public string CurrentPassword { get; set; } = string.Empty;
    [SafeString(300)]
    public string NewPassword { get; set; } = string.Empty;
}
