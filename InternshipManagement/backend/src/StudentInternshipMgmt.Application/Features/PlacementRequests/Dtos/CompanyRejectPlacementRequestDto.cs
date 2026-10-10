namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

/// <summary>Reason supplied when a company rejects a placement request.</summary>
public class CompanyRejectPlacementRequestDto
{
    public string Reason { get; set; } = string.Empty;
}
