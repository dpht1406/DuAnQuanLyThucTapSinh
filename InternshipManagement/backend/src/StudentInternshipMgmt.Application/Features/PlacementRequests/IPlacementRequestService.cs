using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests;

public interface IPlacementRequestService
{
    Task<(bool Success, string? Error, PlacementRequestDto? Data)> CreateRequestAsync(int studentId, CreatePlacementRequestDto dto);

    Task<PagedResult<PlacementRequestDto>> GetRequestsAsync(PlacementRequestFilterDto filter);

    Task<PagedResult<CompanyPlacementRequestDto>> GetCompanyRequestsAsync(
        int companyId,
        CompanyPlacementRequestFilterDto filter);

    Task<CompanyPlacementRequestDto?> GetCompanyRequestByIdAsync(int companyId, int requestId);

    Task<(bool Success, string? Error, bool NotFound)> InviteCompanyRequestToInterviewAsync(
        int companyId,
        int requestId,
        InviteInterviewDto dto,
        int companyUserId);

    Task<(bool Success, string? Error, bool NotFound)> RejectCompanyRequestAsync(
        int companyId,
        int requestId,
        CompanyRejectPlacementRequestDto dto,
        int companyUserId);

    Task<PlacementRequestOperationResult> RecordCompanyResultAsync(
        int companyId,
        int requestId,
        CompanyPlacementResultDto dto,
        int companyUserId);

    Task<List<PlacementRequestDto>> GetMyRequestsAsync(int studentId);

    Task<(bool Success, string? Error, bool NotFound)> ReportInterviewedAsync(int studentId, int requestId, ReportInterviewDto dto);

    Task<PagedResult<PlacementRequestDto>> GetPendingConfirmationAsync(PlacementRequestFilterDto filter);

    Task<PlacementRequestOperationResult> AdminConfirmResultAsync(
        int requestId,
        int adminUserId,
        AdminPlacementResultDto dto);

    Task<PlacementRequestOperationResult> CancelAcceptedResultAsync(int requestId, int adminUserId, string? note = null);
}
