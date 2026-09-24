using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.Students;

public class StudentFilterDto
{
    public string? Search { get; set; }          // theo StudentCode hoặc FullName, contains, ko phân biệt hoa thường
    public StudentStatus? Status { get; set; }
    public int? CompanyId { get; set; }
    public bool? HasAccount { get; set; }        // null = tất cả, true = đã có tài khoản, false = chưa có
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
