using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

/// <summary>Placement request details visible to its target company.</summary>
public class CompanyPlacementRequestDto
{
    public int Id { get; set; }
    public int? JobPositionId { get; set; }
    public string? JobPositionTitle { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public StudentStatus StudentStatus { get; set; }
    public string? ApplicantFullName { get; set; }
    public string? ApplicantEmail { get; set; }
    public string? ApplicantPhone { get; set; }
    public string? ApplicantSchool { get; set; }
    public string? ApplicantMajor { get; set; }
    public string? CvUrl { get; set; }
    public string? CoverLetter { get; set; }
    public string? Note { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? InterviewAt { get; set; }
    public string? InterviewLocation { get; set; }
    public string? InterviewNote { get; set; }
    public string? RejectReason { get; set; }
    public string? ResultNote { get; set; }
    public ActorType? ResultActorType { get; set; }
}
