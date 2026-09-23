using StudentInternshipMgmt.Domain.Common;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Domain.Entities;

public class StatusHistory : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public StudentStatus FromStatus { get; set; }
    public StudentStatus ToStatus { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }

    public int ChangedBy { get; set; }
    public User ChangedByUser { get; set; } = null!;
}
