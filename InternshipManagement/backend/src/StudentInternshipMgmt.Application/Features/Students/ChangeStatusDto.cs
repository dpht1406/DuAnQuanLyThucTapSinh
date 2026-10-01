using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.Students;

public class ChangeStatusDto
{
    public StudentStatus NewStatus { get; set; }
    public string? Note { get; set; }
    public string? RejectReason { get; set; }
}

public class StatusHistoryDto
{
    public int Id { get; set; }
    public StudentStatus FromStatus { get; set; }
    public StudentStatus ToStatus { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedByUsername { get; set; } = default!;
}
