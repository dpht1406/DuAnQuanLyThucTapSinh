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

    public string? ApplicantFullName { get; set; }
    public string? ApplicantEmail { get; set; }
    public string? ApplicantPhone { get; set; }
    public string? ApplicantSchool { get; set; }
    public string? ApplicantMajor { get; set; }
    public string? CvUrl { get; set; }
    public string? CoverLetter { get; set; }

    public RequestStatus Status { get; set; }
    public string? Note { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? ReviewedBy { get; set; }
    public string? ReviewedByUsername { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
