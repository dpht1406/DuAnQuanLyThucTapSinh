namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

/// <summary>Interview details supplied when inviting an applicant.</summary>
public class InviteInterviewDto
{
    public DateTime InterviewAt { get; set; }
    public string InterviewLocation { get; set; } = string.Empty;
    public string? InterviewNote { get; set; }
}
