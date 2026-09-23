using StudentInternshipMgmt.Domain.Common;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Domain.Entities;

public class Student : BaseEntity
{
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public StudentStatus Status { get; set; } = StudentStatus.NoCompany;

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? JobPositionId { get; set; }
    public JobPosition? JobPosition { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
    public ICollection<PlacementRequest> PlacementRequests { get; set; } = new List<PlacementRequest>();
}
