using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.JobPositions;
using StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/job-positions")]
[Authorize]
public class JobPositionsController : ControllerBase
{
    private readonly IJobPositionService _jobPositionService;

    public JobPositionsController(IJobPositionService jobPositionService)
    {
        _jobPositionService = jobPositionService;
    }

    // GET /api/job-positions — Admin + User.
    // Admin -> PagedResult<JobPositionAdminDto> (có AcceptedCount).
    // User  -> PagedResult<JobPositionDto> (không có AcceptedCount).
    [Authorize(Roles = "Admin,User")]
    [HttpGet]
    public async Task<IActionResult> GetJobPositions([FromQuery] JobPositionFilterDto filter)
    {
        if (User.IsInRole("Admin"))
        {
            var adminResult = await _jobPositionService.GetJobPositionsAdminAsync(filter);
            return Ok(ApiResponse<PagedResult<JobPositionAdminDto>>.SuccessResponse(adminResult));
        }

        var result = await _jobPositionService.GetJobPositionsAsync(filter);
        return Ok(ApiResponse<PagedResult<JobPositionDto>>.SuccessResponse(result));
    }

    // GET /api/job-positions/{id} — Admin + User, cùng nguyên tắc phân biệt DTO như trên.
    [Authorize(Roles = "Admin,User")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetJobPositionById(int id)
    {
        if (User.IsInRole("Admin"))
        {
            var adminData = await _jobPositionService.GetJobPositionByIdAdminAsync(id);
            if (adminData is null)
                return NotFound(ApiResponse<JobPositionAdminDto>.FailResponse("Không tìm thấy vị trí tuyển dụng."));

            return Ok(ApiResponse<JobPositionAdminDto>.SuccessResponse(adminData));
        }

        var data = await _jobPositionService.GetJobPositionByIdAsync(id);
        if (data is null)
            return NotFound(ApiResponse<JobPositionDto>.FailResponse("Không tìm thấy vị trí tuyển dụng."));

        return Ok(ApiResponse<JobPositionDto>.SuccessResponse(data));
    }

    // POST /api/job-positions — Admin only.
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobPositionDto>>> CreateJobPosition([FromBody] CreateJobPositionDto dto)
    {
        var (success, error, data) = await _jobPositionService.CreateJobPositionAsync(dto);
        if (!success)
            return BadRequest(ApiResponse<JobPositionDto>.FailResponse(error!));

        return Ok(ApiResponse<JobPositionDto>.SuccessResponse(data!, "Tạo vị trí tuyển dụng thành công."));
    }

    // PUT /api/job-positions/{id} — Admin only.
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateJobPosition(int id, [FromBody] UpdateJobPositionDto dto)
    {
        var (success, error, notFound) = await _jobPositionService.UpdateJobPositionAsync(id, dto);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật vị trí tuyển dụng thành công."));
    }

    // DELETE /api/job-positions/{id} — Admin only.
    // Chặn xóa cứng nếu còn Student (chưa xóa mềm) gắn với vị trí này, bất kể Status.
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteJobPosition(int id)
    {
        var (success, error, notFound) = await _jobPositionService.DeleteJobPositionAsync(id);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : Conflict(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa vị trí tuyển dụng thành công."));
    }

    // PATCH /api/job-positions/{id}/toggle-open — Admin only.
    // Body tùy chọn { "isOpen": true|false }; nếu không gửi (hoặc gửi null) thì đảo
    // giá trị IsOpen hiện tại. Cho phép đổi bất cứ lúc nào, kể cả khi còn Student gắn vào.
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/toggle-open")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleOpen(int id, [FromBody] ToggleOpenRequest? request)
    {
        var (success, error, notFound) = await _jobPositionService.ToggleOpenAsync(id, request?.IsOpen);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật trạng thái tuyển thành công."));
    }

    // Request tối giản cho toggle-open — không đặt trong thư mục Dtos vì yêu cầu
    // "không cần DTO", chỉ nhận đúng 1 field tùy chọn.
    public class ToggleOpenRequest
    {
        public bool? IsOpen { get; set; }
    }
}
