using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Students;

// Không có Status, CompanyId, JobPositionId — các trường này chỉ đổi qua
// endpoint change-status / gán doanh nghiệp riêng (không thuộc DTO này).
public class CreateStudentDto
{
    [SafeString(20)]
    public string StudentCode { get; set; } = default!;
    [SafeString(150)]
    public string FullName { get; set; } = default!;
    [SafeString(150, trimBeforeLength: true)]
    public string Email { get; set; } = default!;
    [SafeString(20)]
    public string PhoneNumber { get; set; } = default!;
    [SafeString(150)]
    public string Major { get; set; } = default!;
    [SafeString(50)]
    public string ClassName { get; set; } = default!;
}

public class UpdateStudentDto
{
    // Cố tình KHÔNG có StudentCode — không cho sửa mã sinh viên sau khi tạo.
    // Nếu client lỡ gửi StudentCode lên, controller/service sẽ bỏ qua field này.
    [SafeString(150)]
    public string FullName { get; set; } = default!;
    [SafeString(150, trimBeforeLength: true)]
    public string Email { get; set; } = default!;
    [SafeString(20)]
    public string PhoneNumber { get; set; } = default!;
    [SafeString(150)]
    public string Major { get; set; } = default!;
    [SafeString(50)]
    public string ClassName { get; set; } = default!;
}
