namespace StudentInternshipMgmt.Application.Features.PlacementRequests;

public enum PlacementRequestOperationStatus
{
    Success,
    NotFound,
    BusinessError,
    Conflict
}

public sealed record PlacementRequestOperationResult(
    PlacementRequestOperationStatus Status,
    string? Error = null);
