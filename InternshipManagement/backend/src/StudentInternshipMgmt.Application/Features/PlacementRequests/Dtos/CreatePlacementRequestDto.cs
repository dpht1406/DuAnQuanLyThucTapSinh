namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class CreatePlacementRequestDto
{
    public int CompanyId { get; set; }
    public int? JobPositionId { get; set; }
    public string? Note { get; set; }
}
