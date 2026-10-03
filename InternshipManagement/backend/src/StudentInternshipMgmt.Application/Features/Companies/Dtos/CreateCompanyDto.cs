using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Companies.Dtos;

// Không dùng [Required]/[MaxLength] — validate thủ công trong CompanyService,
// đi theo đúng pattern Service trả tuple (success, error) của StudentService,
// để không trộn 2 kiểu validate trong cùng codebase.
public class CreateCompanyDto
{
    [SafeString(200)]
    public string Name { get; set; } = string.Empty;
    [SafeString(300)]
    public string Address { get; set; } = string.Empty;
    [SafeString(150)]
    public string Industry { get; set; } = string.Empty;
    [SafeString(150)]
    public string ContactPerson { get; set; } = string.Empty;
    [SafeString(20)]
    public string? ContactPhone { get; set; }
    [SafeString(150, trimBeforeLength: true)]
    public string? ContactEmail { get; set; }
    [SafeString(100)]
    public string? ContactPosition { get; set; }
}
