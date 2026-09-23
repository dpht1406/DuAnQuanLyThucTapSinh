using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class PlacementRequestDto
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public string StudentCode { get; set; } = default!;
    public string StudentFullName { get; set; } = default!;

    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = default!;

    public int? JobPositionId { get; set; }
    public string? JobPositionTitle { get; set; }

    public RequestStatus Status { get; set; }
    public string? Note { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? ReviewedBy { get; set; }
    public string? ReviewedByUsername { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
