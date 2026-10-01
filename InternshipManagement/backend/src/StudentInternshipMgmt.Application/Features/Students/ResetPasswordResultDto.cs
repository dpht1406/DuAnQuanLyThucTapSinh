namespace StudentInternshipMgmt.Application.Features.Students;

public class ResetPasswordResultDto
{
    public string StudentCode { get; set; } = default!;

    // Mật khẩu plain text chỉ tồn tại trong response này; không lưu DB và không log.
    public string TemporaryPassword { get; set; } = default!;
}