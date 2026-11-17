using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.About;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/about")]
[Authorize]
public class AboutController : ControllerBase
{
    private readonly IAboutService _aboutService;

    public AboutController(IAboutService aboutService)
    {
        _aboutService = aboutService;
    }

    // GET /api/about — Mọi tài khoản đã đăng nhập (Admin, User, Company)
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AboutPageDto>>> GetAbout()
    {
        var result = await _aboutService.GetAboutAsync();
        return Ok(ApiResponse<AboutPageDto>.SuccessResponse(result));
    }

    // PUT /api/about — Chỉ Admin
    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<ActionResult<ApiResponse<AboutPageDto>>> UpdateAbout([FromBody] AboutContentDto content)
    {
        var userId = GetCurrentUserId();
        var (success, errors, data) = await _aboutService.UpdateAboutAsync(content, userId);
        if (!success)
        {
            return BadRequest(ApiResponse<AboutPageDto>.FailResponse("Dữ liệu không hợp lệ.", errors));
        }

        return Ok(ApiResponse<AboutPageDto>.SuccessResponse(data, "Cập nhật trang Giới thiệu thành công."));
    }

    // POST /api/about/reset — Chỉ Admin
    [Authorize(Roles = "Admin")]
    [HttpPost("reset")]
    public async Task<ActionResult<ApiResponse<AboutPageDto>>> ResetAbout()
    {
        var userId = GetCurrentUserId();
        var result = await _aboutService.ResetAboutAsync(userId);
        return Ok(ApiResponse<AboutPageDto>.SuccessResponse(result, "Khôi phục nội dung mặc định thành công."));
    }

    private int? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}
