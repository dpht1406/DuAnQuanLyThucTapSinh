using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests;

public interface IPlacementRequestService
{
    Task<(bool Success, string? Error, PlacementRequestDto? Data)> CreateRequestAsync(int studentId, CreatePlacementRequestDto dto);

    Task<PagedResult<PlacementRequestDto>> GetRequestsAsync(PlacementRequestFilterDto filter);

    Task<List<PlacementRequestDto>> GetMyRequestsAsync(int studentId);

    Task<(bool Success, string? Error, bool NotFound)> ApproveRequestAsync(int requestId, int adminUserId);

    Task<(bool Success, string? Error, bool NotFound)> RejectRequestAsync(int requestId, RejectPlacementRequestDto dto, int adminUserId);
}
