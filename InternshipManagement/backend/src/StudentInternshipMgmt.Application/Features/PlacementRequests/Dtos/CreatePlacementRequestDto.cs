namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class CreatePlacementRequestDto
{
    public int CompanyId { get; set; }
    public int? JobPositionId { get; set; }
    public string ApplicantFullName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string ApplicantPhone { get; set; } = string.Empty;
    public string ApplicantSchool { get; set; } = string.Empty;
    public string ApplicantMajor { get; set; } = string.Empty;
    public string CvUrl { get; set; } = string.Empty;
    public string CoverLetter { get; set; } = string.Empty;
    public string? Note { get; set; }
}
