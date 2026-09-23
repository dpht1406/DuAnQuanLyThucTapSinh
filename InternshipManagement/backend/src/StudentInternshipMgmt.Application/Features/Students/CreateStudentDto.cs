namespace StudentInternshipMgmt.Application.Features.Students;

// Không có Status, CompanyId, JobPositionId — các trường này chỉ đổi qua
// endpoint change-status / gán doanh nghiệp riêng (không thuộc DTO này).
public class CreateStudentDto
{
    public string StudentCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Major { get; set; } = default!;
    public string ClassName { get; set; } = default!;
}

public class UpdateStudentDto
{
    // Cố tình KHÔNG có StudentCode — không cho sửa mã sinh viên sau khi tạo.
    // Nếu client lỡ gửi StudentCode lên, controller/service sẽ bỏ qua field này.
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Major { get; set; } = default!;
    public string ClassName { get; set; } = default!;
}
