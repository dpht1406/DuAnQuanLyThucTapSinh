namespace StudentInternshipMgmt.Application.Features.PlacementRequests;

public interface IPlacementRequestExpiryService
{
    Task<int> ExpireOverdueAsync(DateTime utcNow, CancellationToken ct);
}
