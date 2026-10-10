using StudentInternshipMgmt.Domain.Common;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Domain.Entities;

public class JobPosition : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime? Deadline { get; set; }
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public JobPositionApprovalStatus ApprovalStatus { get; set; } = JobPositionApprovalStatus.Approved;
    public string? ApprovalNote { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
