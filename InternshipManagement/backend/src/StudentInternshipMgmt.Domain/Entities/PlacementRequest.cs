using StudentInternshipMgmt.Domain.Common;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Domain.Entities;

public class PlacementRequest : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int? JobPositionId { get; set; }
    public JobPosition? JobPosition { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Note { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? ReviewedBy { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
