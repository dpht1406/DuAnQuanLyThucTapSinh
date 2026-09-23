using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.Students;

public class StudentDto
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Major { get; set; } = default!;
    public string ClassName { get; set; } = default!;
    public StudentStatus Status { get; set; }

    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? JobPositionId { get; set; }
    public string? JobPositionTitle { get; set; }

    public bool HasAccount { get; set; }
    public DateTime CreatedAt { get; set; }
}
