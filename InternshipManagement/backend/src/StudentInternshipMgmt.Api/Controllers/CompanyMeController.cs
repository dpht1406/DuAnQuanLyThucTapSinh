using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Companies;
using StudentInternshipMgmt.Application.Features.Companies.Dtos;
using StudentInternshipMgmt.Application.Features.JobPositions;
using StudentInternshipMgmt.Application.Features.JobPositions.Dtos;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using System.Globalization;
using System.Security.Claims;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/company-me")]
[Authorize(Roles = "Company")]
public class CompanyMeController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly IJobPositionService _jobPositionService;
    private readonly IPlacementRequestService _placementRequestService;

    public CompanyMeController(
        ICompanyService companyService,
        IJobPositionService jobPositionService,
        IPlacementRequestService placementRequestService)
    {
        _companyService = companyService;
        _jobPositionService = jobPositionService;
        _placementRequestService = placementRequestService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> GetMyProfile()
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<CompanyDto>.FailResponse("Tài khoản chưa được gắn với doanh nghiệp."));

        var company = await _companyService.GetMyCompanyAsync(companyId);
        if (company is null)
            return NotFound(ApiResponse<CompanyDto>.FailResponse("Không tìm thấy công ty."));

        return Ok(ApiResponse<CompanyDto>.SuccessResponse(company));
    }

    [HttpGet("job-positions")]
    public async Task<ActionResult<ApiResponse<PagedResult<JobPositionAdminDto>>>> GetMyJobPositions(
        [FromQuery] JobPositionFilterDto filter)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<PagedResult<JobPositionAdminDto>>.FailResponse(
                "Tài khoản chưa được gắn với doanh nghiệp."));

        var positions = await _jobPositionService.GetJobPositionsForCompanyAsync(companyId, filter);
        return Ok(ApiResponse<PagedResult<JobPositionAdminDto>>.SuccessResponse(positions));
    }

    [HttpGet("job-positions/{id:int}")]
    public async Task<ActionResult<ApiResponse<JobPositionAdminDto>>> GetMyJobPosition(int id)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<JobPositionAdminDto>.FailResponse(
                "Tài khoản chưa được gắn với doanh nghiệp."));

        var position = await _jobPositionService.GetJobPositionByIdForCompanyAsync(companyId, id);
        if (position is null)
            return NotFound(ApiResponse<JobPositionAdminDto>.FailResponse("Không tìm thấy vị trí tuyển dụng."));

        return Ok(ApiResponse<JobPositionAdminDto>.SuccessResponse(position));
    }

    [HttpPost("job-positions")]
    public async Task<ActionResult<ApiResponse<JobPositionAdminDto>>> CreateJobPosition(
        [FromBody] CreateCompanyJobPositionDto dto)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<JobPositionAdminDto>.FailResponse(
                "Tài khoản chưa được gắn với doanh nghiệp."));

        var (success, error, data) = await _jobPositionService.CreateJobPositionForCompanyAsync(companyId, dto);
        if (!success)
            return BadRequest(ApiResponse<JobPositionAdminDto>.FailResponse(error!));

        return CreatedAtAction(
            nameof(GetMyJobPosition),
            new { id = data!.Id },
            ApiResponse<JobPositionAdminDto>.SuccessResponse(data, "Tạo vị trí tuyển dụng thành công."));
    }

    [HttpPut("job-positions/{id:int}")]
    public async Task<ActionResult<ApiResponse<JobPositionAdminDto>>> UpdateJobPosition(
        int id, [FromBody] UpdateCompanyJobPositionDto dto)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<JobPositionAdminDto>.FailResponse(
                "Tài khoản chưa được gắn với doanh nghiệp."));

        var (success, error, notFound, data) =
            await _jobPositionService.UpdateJobPositionForCompanyAsync(companyId, id, dto);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<JobPositionAdminDto>.FailResponse(error!))
                : BadRequest(ApiResponse<JobPositionAdminDto>.FailResponse(error!));

        return Ok(ApiResponse<JobPositionAdminDto>.SuccessResponse(data, "Cập nhật vị trí tuyển dụng thành công."));
    }

    [HttpPost("job-positions/{id:int}/submit")]
    public async Task<ActionResult<ApiResponse<object>>> SubmitForApproval(int id)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<object>.FailResponse("Tài khoản chưa được gắn với doanh nghiệp."));

        var (success, error, notFound) = await _jobPositionService.SubmitForApprovalAsync(companyId, id);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đã gửi vị trí tuyển dụng để duyệt."));
    }

    [HttpPatch("job-positions/{id:int}/toggle-open")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleOpen(
        int id, [FromBody] ToggleOpenRequest? request)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<object>.FailResponse("Tài khoản chưa được gắn với doanh nghiệp."));

        var (success, error, notFound) =
            await _jobPositionService.ToggleOpenForCompanyAsync(companyId, id, request?.IsOpen);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật trạng thái tuyển thành công."));
    }

    [HttpDelete("job-positions/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteJobPosition(int id)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<object>.FailResponse("Tài khoản chưa được gắn với doanh nghiệp."));

        var (success, error, notFound) = await _jobPositionService.DeleteJobPositionForCompanyAsync(companyId, id);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : Conflict(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa vị trí tuyển dụng thành công."));
    }

    [HttpGet("placement-requests")]
    public async Task<ActionResult<ApiResponse<PagedResult<CompanyPlacementRequestDto>>>> GetMyPlacementRequests(
        [FromQuery] CompanyPlacementRequestFilterDto filter)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<PagedResult<CompanyPlacementRequestDto>>.FailResponse(
                "Tài khoản chưa được gắn với doanh nghiệp."));

        var requests = await _placementRequestService.GetCompanyRequestsAsync(companyId, filter);
        return Ok(ApiResponse<PagedResult<CompanyPlacementRequestDto>>.SuccessResponse(requests));
    }

    [HttpGet("placement-requests/{id:int}")]
    public async Task<ActionResult<ApiResponse<CompanyPlacementRequestDto>>> GetMyPlacementRequest(int id)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<CompanyPlacementRequestDto>.FailResponse(
                "Tài khoản chưa được gắn với doanh nghiệp."));

        var request = await _placementRequestService.GetCompanyRequestByIdAsync(companyId, id);
        if (request is null)
            return NotFound(ApiResponse<CompanyPlacementRequestDto>.FailResponse("Không tìm thấy đơn ứng tuyển."));

        return Ok(ApiResponse<CompanyPlacementRequestDto>.SuccessResponse(request));
    }

    [HttpPost("placement-requests/{id:int}/invite-interview")]
    public async Task<ActionResult<ApiResponse<object>>> InviteToInterview(
        int id,
        [FromBody] InviteInterviewDto dto)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<object>.FailResponse("Tài khoản chưa được gắn với doanh nghiệp."));

        var companyUserId = GetCurrentCompanyUserId();
        if (companyUserId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error, notFound) = await _placementRequestService.InviteCompanyRequestToInterviewAsync(
            companyId, id, dto, companyUserId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đã gửi lời mời phỏng vấn."));
    }

    [HttpPost("placement-requests/{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<object>>> RejectPlacementRequest(
        int id,
        [FromBody] CompanyRejectPlacementRequestDto dto)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<object>.FailResponse("Tài khoản chưa được gắn với doanh nghiệp."));

        var companyUserId = GetCurrentCompanyUserId();
        if (companyUserId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error, notFound) = await _placementRequestService.RejectCompanyRequestAsync(
            companyId, id, dto, companyUserId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đã từ chối đơn ứng tuyển."));
    }

    [HttpPost("placement-requests/{id:int}/result")]
    public async Task<ActionResult<ApiResponse<object>>> RecordPlacementResult(
        int id,
        [FromBody] CompanyPlacementResultDto dto)
    {
        if (!TryGetCompanyId(out var companyId))
            return Unauthorized(ApiResponse<object>.FailResponse("Tài khoản chưa được gắn với doanh nghiệp."));

        var companyUserId = GetCurrentCompanyUserId();
        if (companyUserId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var result = await _placementRequestService.RecordCompanyResultAsync(
            companyId, id, dto, companyUserId.Value);
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
                throw new InvalidOperationException(
                    $"Unexpected placement request result status: {result.Status}.");
        }

        var message = dto.IsPassed
            ? "Đã ghi nhận kết quả Đạt và nhận sinh viên."
            : "Đã ghi nhận kết quả Không đạt.";
        return Ok(ApiResponse<object>.SuccessResponse(null!, message));
    }

    private bool TryGetCompanyId(out int companyId)
    {
        return int.TryParse(User.FindFirst("CompanyId")?.Value, out companyId);
    }

    private int? GetCurrentCompanyUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    public class ToggleOpenRequest
    {
        public bool? IsOpen { get; set; }
    }
}
