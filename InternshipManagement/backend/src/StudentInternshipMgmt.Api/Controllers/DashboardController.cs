using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Dashboard;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // GET /api/dashboard/summary
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary()
    {
        var result = await _dashboardService.GetSummaryAsync();
        return Ok(ApiResponse<DashboardSummaryDto>.SuccessResponse(result));
    }

    // GET /api/dashboard/need-assignment
    [HttpGet("need-assignment")]
    public async Task<ActionResult<ApiResponse<List<NeedAssignmentDto>>>> GetNeedAssignmentList()
    {
        var result = await _dashboardService.GetNeedAssignmentListAsync();
        return Ok(ApiResponse<List<NeedAssignmentDto>>.SuccessResponse(result));
    }

    // GET /api/dashboard/recent-rejections
    [HttpGet("recent-rejections")]
    public async Task<ActionResult<ApiResponse<List<RecentRejectionDto>>>> GetRecentRejections()
    {
        var result = await _dashboardService.GetRecentRejectionsAsync();
        return Ok(ApiResponse<List<RecentRejectionDto>>.SuccessResponse(result));
    }

    // GET /api/dashboard/overdue-requests
    [HttpGet("overdue-requests")]
    public async Task<ActionResult<ApiResponse<List<PlacementRequestDto>>>> GetOverdueRequests()
    {
        var result = await _dashboardService.GetOverdueRequestsAsync();
        return Ok(ApiResponse<List<PlacementRequestDto>>.SuccessResponse(result));
    }

    // GET /api/dashboard/pending-interview-results
    [HttpGet("pending-interview-results")]
    public async Task<ActionResult<ApiResponse<List<PlacementRequestDto>>>> GetPendingInterviewResults()
    {
        var result = await _dashboardService.GetPendingInterviewResultsAsync();
        return Ok(ApiResponse<List<PlacementRequestDto>>.SuccessResponse(result));
    }
}
