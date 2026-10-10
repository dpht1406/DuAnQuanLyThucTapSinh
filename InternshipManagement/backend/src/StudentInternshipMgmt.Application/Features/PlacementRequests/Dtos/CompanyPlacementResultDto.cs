namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

/// <summary>Result submitted by a company for a placement request.</summary>
public sealed record CompanyPlacementResultDto
{
    public bool IsPassed { get; init; }
    public string? Note { get; init; }
}
