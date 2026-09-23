using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
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

    // POST /api/placement-requests/{id}/approve
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveRequest(int id)
    {
        var adminId = GetCurrentAdminId();
        if (adminId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error, notFound) = await _placementRequestService.ApproveRequestAsync(id, adminId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Duyệt yêu cầu thành công."));
    }

    // POST /api/placement-requests/{id}/reject
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<object>>> RejectRequest(int id, [FromBody] RejectPlacementRequestDto dto)
    {
        var adminId = GetCurrentAdminId();
        if (adminId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error, notFound) = await _placementRequestService.RejectRequestAsync(id, dto, adminId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Từ chối yêu cầu thành công."));
    }

    // Lấy UserId của admin đang đăng nhập từ JWT claim.
    // Giống cách GetCurrentAdminId() trong StudentsController.
    private int? GetCurrentAdminId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}
