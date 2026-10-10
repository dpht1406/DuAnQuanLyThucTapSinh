using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Students;

public class StudentFilterDto
{
    [SafeString(150)]
    public string? Search { get; set; }          // Trim; có @ tìm Email, có số tìm StudentCode, còn lại tìm FullName; contains, CI_AI (đ/Đ vẫn khác d/D)
    public StudentStatus? Status { get; set; }
    public int? CompanyId { get; set; }
    public bool? HasAccount { get; set; }        // null = tất cả, true = đã có tài khoản, false = chưa có
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
