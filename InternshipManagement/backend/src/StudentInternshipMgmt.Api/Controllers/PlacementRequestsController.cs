using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Enums;
using System.Globalization;
using System.Security.Claims;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/placement-requests")]
[Authorize(Roles = "Admin")]
public class PlacementRequestsController : ControllerBase
{
    private readonly IPlacementRequestService _placementRequestService;

    public PlacementRequestsController(IPlacementRequestService placementRequestService)
    {
        _placementRequestService = placementRequestService;
    }

    // GET /api/placement-requests
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PlacementRequestDto>>>> GetRequests([FromQuery] PlacementRequestFilterDto filter)
    {
        var result = await _placementRequestService.GetRequestsAsync(filter);
        return Ok(ApiResponse<PagedResult<PlacementRequestDto>>.SuccessResponse(result));
    }

    // GET /api/placement-requests/pending-confirmation
    [HttpGet("pending-confirmation")]
    public async Task<ActionResult<ApiResponse<PagedResult<PlacementRequestDto>>>> GetPendingConfirmation(
        [FromQuery] PlacementRequestFilterDto filter)
    {
        var result = await _placementRequestService.GetPendingConfirmationAsync(filter);
        return Ok(ApiResponse<PagedResult<PlacementRequestDto>>.SuccessResponse(result));
    }

    // POST /api/placement-requests/{id}/admin-confirm-result
    [HttpPost("{id:int}/admin-confirm-result")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmAdminResult(
        int id,
        [FromBody] AdminPlacementResultDto dto)
    {
        var adminId = GetCurrentAdminId();
        if (adminId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var result = await _placementRequestService.AdminConfirmResultAsync(id, adminId.Value, dto);
        switch (result.Status)
        {
            case PlacementRequestOperationStatus.NotFound:
                return NotFound(ApiResponse<object>.FailResponse(result.Error!));
            case PlacementRequestOperationStatus.BusinessError:
                return BadRequest(ApiResponse<object>.FailResponse(result.Error!));
            case PlacementRequestOperationStatus.Conflict:
                return Conflict(ApiResponse<object>.FailResponse(result.Error!));
            case PlacementRequestOperationStatus.Success:
                break;
            default:
                throw new InvalidOperationException($"Unexpected placement request result status: {result.Status}.");
        }

        var message = dto.Decision switch
        {
            AdminPlacementDecision.Passed => "Đã ghi nhận kết quả Đạt thay doanh nghiệp.",
            AdminPlacementDecision.Failed => "Đã ghi nhận kết quả Không đạt.",
            AdminPlacementDecision.DismissReport => "Đã bác báo cáo phỏng vấn của sinh viên.",
            _ => "Đã cập nhật kết quả phỏng vấn."
        };

        return Ok(ApiResponse<object>.SuccessResponse(null!, message));
    }

    // POST /api/placement-requests/{id}/cancel-accepted-result
    [HttpPost("{id:int}/cancel-accepted-result")]
    public async Task<ActionResult<ApiResponse<object>>> CancelAcceptedResult(int id, [FromBody] CancelAcceptedResultRequest? request)
    {
        var adminId = GetCurrentAdminId();
        if (adminId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var result = await _placementRequestService.CancelAcceptedResultAsync(id, adminId.Value, request?.Note);
        switch (result.Status)
        {
            case PlacementRequestOperationStatus.NotFound:
                return NotFound(ApiResponse<object>.FailResponse(result.Error!));
            case PlacementRequestOperationStatus.BusinessError:
                return BadRequest(ApiResponse<object>.FailResponse(result.Error!));
            case PlacementRequestOperationStatus.Conflict:
                return Conflict(ApiResponse<object>.FailResponse(result.Error!));
            case PlacementRequestOperationStatus.Success:
                break;
            default:
                throw new InvalidOperationException($"Unexpected placement request result status: {result.Status}.");
        }

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đã hủy kết quả tiếp nhận sinh viên."));
    }

    // Lấy UserId của admin đang đăng nhập từ JWT claim.
    // Giống cách GetCurrentAdminId() trong StudentsController.
    private int? GetCurrentAdminId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    public class CancelAcceptedResultRequest
    {
        public string? Note { get; set; }
    }
}
