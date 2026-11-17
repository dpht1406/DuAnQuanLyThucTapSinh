using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Auth;
using StudentInternshipMgmt.Application.Features.Auth.Dtos;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILoginAttemptTracker _loginAttemptTracker;

    public AuthController(IAuthService authService, ILoginAttemptTracker loginAttemptTracker)
    {
        _authService = authService;
        _loginAttemptTracker = loginAttemptTracker;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!_loginAttemptTracker.TryBeginAttempt(request.Username, ipAddress, out var retryAfter))
        {
            var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            var retryAfterMinutes = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                ApiResponse<LoginResponseDto>.FailResponse(
                    $"Bạn đã nhập sai quá nhiều lần. Vui lòng thử lại sau {retryAfterMinutes} phút."));
        }

        LoginResponseDto response;
        try
        {
            response = await _authService.LoginAsync(request, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            _loginAttemptTracker.RecordFailedAttempt(request.Username, ipAddress);
            throw;
        }
        catch
        {
            _loginAttemptTracker.CancelAttempt(request.Username, ipAddress);
            throw;
        }

        _loginAttemptTracker.RecordSuccessfulAttempt(request.Username, ipAddress);
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, "Đăng nhập thành công."));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, "Làm mới token thành công."));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Đổi mật khẩu thành công."));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Đăng xuất thành công."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<MeResponseDto>>> Me(CancellationToken cancellationToken)
    {
        var data = await _authService.GetMeAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse<MeResponseDto>.SuccessResponse(data));
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (!int.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedAccessException("Token không hợp lệ.");
        }

        return parsedUserId;
    }
}
