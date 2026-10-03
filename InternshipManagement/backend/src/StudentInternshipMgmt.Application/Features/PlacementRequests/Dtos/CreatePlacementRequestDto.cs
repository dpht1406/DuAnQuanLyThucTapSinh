using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class CreatePlacementRequestDto
{
    public int CompanyId { get; set; }
    public int? JobPositionId { get; set; }
    [SafeString(100)]
    public string ApplicantFullName { get; set; } = string.Empty;
    [SafeString(254, trimBeforeLength: true)]
    public string ApplicantEmail { get; set; } = string.Empty;
    [SafeString(10)]
    public string ApplicantPhone { get; set; } = string.Empty;
    [SafeString(150)]
    public string ApplicantSchool { get; set; } = string.Empty;
    [SafeString(150)]
    public string ApplicantMajor { get; set; } = string.Empty;
    [SafeString(500)]
    public string CvUrl { get; set; } = string.Empty;
    [SafeString(2000, allowLineBreaks: true)]
    public string CoverLetter { get; set; } = string.Empty;
    [SafeString(1000, allowLineBreaks: true)]
    public string? Note { get; set; }
}
