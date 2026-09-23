namespace StudentInternshipMgmt.Application.Features.Companies.Dtos;

// Không dùng [Required]/[MaxLength] — validate thủ công trong CompanyService,
// đi theo đúng pattern Service trả tuple (success, error) của StudentService,
// để không trộn 2 kiểu validate trong cùng codebase.
public class CreateCompanyDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
}
