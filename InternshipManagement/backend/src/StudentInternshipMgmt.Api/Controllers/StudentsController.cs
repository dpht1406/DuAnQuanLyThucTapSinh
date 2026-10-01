using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Students;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace StudentInternshipMgmt.API.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = "Admin")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    // GET /api/students
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<StudentDto>>>> GetStudents([FromQuery] StudentFilterDto filter)
    {
        var result = await _studentService.GetStudentsAsync(filter);
        return Ok(ApiResponse<PagedResult<StudentDto>>.SuccessResponse(result));
    }

    // GET /api/students/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudentById(int id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);
        if (student is null)
            return NotFound(ApiResponse<StudentDto>.FailResponse("Không tìm thấy sinh viên."));

        return Ok(ApiResponse<StudentDto>.SuccessResponse(student));
    }

    // POST /api/students
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudentDto>>> CreateStudent([FromBody] CreateStudentDto dto)
    {
        var (success, error, data) = await _studentService.CreateStudentAsync(dto);
        if (!success)
            return BadRequest(ApiResponse<StudentDto>.FailResponse(error!));

        return Ok(ApiResponse<StudentDto>.SuccessResponse(data!, "Tạo sinh viên thành công."));
    }

    // PUT /api/students/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
    {
        var (success, error) = await _studentService.UpdateStudentAsync(id, dto);
        if (!success)
            return NotFound(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật thành công."));
    }

    // DELETE /api/students/{id}
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteStudent(int id)
    {
        var (success, error) = await _studentService.DeleteStudentAsync(id);
        if (!success)
            return NotFound(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Xoá mềm thành công."));
    }

    // POST /api/students/create-accounts
    [HttpPost("create-accounts")]
    public async Task<ActionResult<ApiResponse<CreateAccountsResultDto>>> CreateAccounts([FromBody] CreateAccountsRequestDto dto)
    {
        if (dto.StudentIds is null || dto.StudentIds.Count == 0)
            return BadRequest(ApiResponse<CreateAccountsResultDto>.FailResponse("Danh sách StudentIds trống."));

        var result = await _studentService.CreateAccountsAsync(dto.StudentIds);
        return Ok(ApiResponse<CreateAccountsResultDto>.SuccessResponse(result));
    }

    // POST /api/students/create-accounts/by-filter
    // Tạo tài khoản cho TẤT CẢ sinh viên khớp filter hiện tại (không cần gửi danh sách Id cụ thể).
    // Tái dùng StudentFilterDto — PageNumber/PageSize bị bỏ qua (không phân trang khi lọc để tạo hàng loạt).
    [HttpPost("create-accounts/by-filter")]
    public async Task<ActionResult<ApiResponse<CreateAccountsResultDto>>> CreateAccountsByFilter([FromBody] StudentFilterDto filter)
    {
        var result = await _studentService.CreateAccountsByFilterAsync(filter);
        return Ok(ApiResponse<CreateAccountsResultDto>.SuccessResponse(result));
    }

    // POST /api/students/accounts/export
    // Đổi từ GET -> POST vì cần nhận lại danh sách (Username, mật khẩu plain) trong body —
    // dữ liệu nhạy cảm không phù hợp để đưa vào query string của GET.
    // Xem ASSUMPTIONS.md mục "Quyết định chủ động #2".
    [HttpPost("accounts/export")]
    public IActionResult ExportAccountsCsv([FromBody] ExportAccountsRequestDto dto)
    {
        if (dto.Accounts is null || dto.Accounts.Count == 0)
            return BadRequest(ApiResponse<object>.FailResponse("Danh sách tài khoản trống."));

        var sb = new StringBuilder();
        sb.AppendLine("Username,PlainPassword");
        foreach (var acc in dto.Accounts)
            sb.AppendLine($"{EscapeCsv(acc.StudentCode)},{EscapeCsv(acc.PlainPassword)}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"accounts_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // GET /api/students/export?format=csv|xlsx&Search=&Status=&CompanyId=
    // Tái sử dụng StudentFilterDto (Search/Status/CompanyId) — bỏ qua PageNumber/PageSize,
    // luôn lấy toàn bộ bản ghi khớp filter. Trả file trực tiếp, không bọc ApiResponse<T>
    // vì đây là dữ liệu nhị phân.
    [HttpGet("export")]
    public async Task<IActionResult> ExportStudents([FromQuery] StudentFilterDto filter, [FromQuery] string format)
    {
        if (string.IsNullOrWhiteSpace(format) ||
            !(format.Equals("csv", StringComparison.OrdinalIgnoreCase) ||
              format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(ApiResponse<object>.FailResponse("Định dạng không hợp lệ. Chỉ hỗ trợ 'csv' hoặc 'xlsx'."));
        }

        var (content, fileName, contentType) = await _studentService.ExportStudentsAsync(filter, format);
        return File(content, contentType, fileName);
    }

    // POST /api/students/import
    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<ImportResultDto>>> ImportStudents(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<ImportResultDto>.FailResponse("File trống hoặc không hợp lệ."));

        using var stream = file.OpenReadStream();
        var result = await _studentService.ImportStudentsAsync(stream, file.FileName);

        return Ok(ApiResponse<ImportResultDto>.SuccessResponse(result));
    }

    // POST /api/students/{id}/change-status
    [HttpPost("{id:int}/change-status")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        var adminId = GetCurrentAdminId();
        if (adminId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error) = await _studentService.ChangeStatusAsync(id, dto, adminId.Value, isAdmin: true);
        if (!success)
            return BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đổi trạng thái thành công."));
    }

    // POST /api/students/{id}/reset-password
    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<ApiResponse<ResetPasswordResultDto>>> ResetPassword(int id)
    {
        Response.Headers["Cache-Control"] = "no-store";
        var adminId = GetCurrentAdminId();
        if (adminId is null)
            return Unauthorized(ApiResponse<ResetPasswordResultDto>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error, data) = await _studentService.ResetPasswordAsync(id, adminId.Value);
        if (!success)
        {
            if (error == "Không tìm thấy sinh viên.")
                return NotFound(ApiResponse<ResetPasswordResultDto>.FailResponse(error));

            return BadRequest(ApiResponse<ResetPasswordResultDto>.FailResponse(error!));
        }

        return Ok(ApiResponse<ResetPasswordResultDto>.SuccessResponse(data!));
    }

    // GET /api/students/{id}/status-history
    [HttpGet("{id:int}/status-history")]
    public async Task<ActionResult<ApiResponse<List<StatusHistoryDto>>>> GetStatusHistory(int id)
    {
        var history = await _studentService.GetStatusHistoryAsync(id);
        if (history is null)
            return NotFound(ApiResponse<List<StatusHistoryDto>>.FailResponse("Không tìm thấy sinh viên."));

        return Ok(ApiResponse<List<StatusHistoryDto>>.SuccessResponse(history));
    }

    // POST /api/students/{id}/assign-company
    [HttpPost("{id:int}/assign-company")]
    public async Task<ActionResult<ApiResponse<object>>> AssignCompany(int id, [FromBody] AssignCompanyDto dto)
    {
        var adminId = GetCurrentAdminId();
        if (adminId is null) return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));
        var (success, error) = await _studentService.AssignCompanyDirectAsync(id, dto, adminId.Value);
        if (!success) return BadRequest(ApiResponse<object>.FailResponse(error!));
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Gán doanh nghiệp thành công."));
    }

    // Lấy UserId của admin đang đăng nhập từ JWT claim.
    // Giả định claim NameIdentifier chứa Id dạng số — xem ASSUMPTIONS.md.
    private int? GetCurrentAdminId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}
