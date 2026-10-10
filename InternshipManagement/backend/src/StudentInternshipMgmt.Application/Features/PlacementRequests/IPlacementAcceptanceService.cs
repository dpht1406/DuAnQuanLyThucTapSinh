using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests;

public interface IPlacementAcceptanceService
{
    Task<PlacementRequestOperationResult> AcceptAsync(
        int requestId,
        int? scopeCompanyId,
        ActorType actor,
        int actorUserId,
        string? note,
        string? confirmationSource);

    Task<PlacementRequestOperationResult> CancelAcceptedAsync(int requestId, int adminUserId, string? note);
}
