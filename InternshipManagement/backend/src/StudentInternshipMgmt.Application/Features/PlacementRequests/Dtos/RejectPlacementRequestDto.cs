using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class RejectPlacementRequestDto
{
    [SafeString(1000, allowLineBreaks: true)]
    public string RejectReason { get; set; } = string.Empty;
}
