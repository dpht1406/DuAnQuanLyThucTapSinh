using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Notifications;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "Admin")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetNotifications(
        [FromQuery] NotificationFilterDto filter)
    {
        var result = await _notificationService.GetNotificationsAsync(filter);
        return Ok(ApiResponse<PagedResult<NotificationDto>>.SuccessResponse(result));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> GetUnreadCount()
    {
        var result = await _notificationService.GetUnreadCountAsync();
        return Ok(ApiResponse<UnreadCountDto>.SuccessResponse(result));
    }

    [HttpPost("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error, notFound) = await _notificationService.MarkAsReadAsync(id, userId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đã đánh dấu thông báo đã đọc."));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        await _notificationService.MarkAllAsReadAsync(userId.Value);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Đã đánh dấu tất cả thông báo đã đọc."));
    }

    private int? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}