using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Students;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using System.Security.Claims;
namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Roles = "User")]
public class MeController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly IPlacementRequestService _placementRequestService;

    public MeController(IStudentService studentService, IPlacementRequestService placementRequestService)
    {
        _studentService = studentService;
        _placementRequestService = placementRequestService;
    }

    // GET /api/me/profile
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetMyProfile()
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized(ApiResponse<StudentDto>.FailResponse("Tài khoản chưa gắn với hồ sơ sinh viên."));

        var student = await _studentService.GetStudentByIdAsync(studentId.Value);
        if (student is null)
            return NotFound(ApiResponse<StudentDto>.FailResponse("Không tìm thấy hồ sơ sinh viên."));

        return Ok(ApiResponse<StudentDto>.SuccessResponse(student));
    }

    // GET /api/me/status-history
    [HttpGet("status-history")]
    public async Task<ActionResult<ApiResponse<List<StatusHistoryDto>>>> GetMyStatusHistory()
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized(ApiResponse<List<StatusHistoryDto>>.FailResponse("Tài khoản chưa gắn với hồ sơ sinh viên."));

        var history = await _studentService.GetStatusHistoryAsync(studentId.Value);
        if (history is null)
            return NotFound(ApiResponse<List<StatusHistoryDto>>.FailResponse("Không tìm thấy hồ sơ sinh viên."));

        return Ok(ApiResponse<List<StatusHistoryDto>>.SuccessResponse(history));
    }

    // POST /api/me/change-status
    [HttpPost("change-status")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeMyStatus([FromBody] ChangeStatusDto dto)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Tài khoản chưa gắn với hồ sơ sinh viên."));

        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error) = await _studentService.ChangeStatusAsync(studentId.Value, dto, userId.Value, isAdmin: false);
        if (!success)
            return BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đổi trạng thái thành công."));
    }

    // GET /api/me/placement-requests
    [HttpGet("placement-requests")]
    public async Task<ActionResult<ApiResponse<List<PlacementRequestDto>>>> GetMyPlacementRequests()
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized(ApiResponse<List<PlacementRequestDto>>.FailResponse("Tài khoản chưa gắn với hồ sơ sinh viên."));

        var requests = await _placementRequestService.GetMyRequestsAsync(studentId.Value);
        return Ok(ApiResponse<List<PlacementRequestDto>>.SuccessResponse(requests));
    }

    // POST /api/me/placement-requests
    [HttpPost("placement-requests")]
    public async Task<ActionResult<ApiResponse<PlacementRequestDto>>> CreateMyPlacementRequest([FromBody] CreatePlacementRequestDto dto)
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized(ApiResponse<PlacementRequestDto>.FailResponse("Tài khoản chưa gắn với hồ sơ sinh viên."));

        var (success, error, data) = await _placementRequestService.CreateRequestAsync(studentId.Value, dto);
        if (!success)
            return BadRequest(ApiResponse<PlacementRequestDto>.FailResponse(error!));

        return Ok(ApiResponse<PlacementRequestDto>.SuccessResponse(data!, "Tạo yêu cầu thành công."));
    }

    // Lấy StudentId từ JWT claim "StudentId" (chỉ user role User mới có claim này).
    private int? GetCurrentStudentId()
    {
        var raw = User.FindFirst("StudentId")?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    // Lấy UserId từ JWT claim NameIdentifier — giống GetCurrentAdminId() trong StudentsController.
    private int? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        return int.TryParse(raw, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}
