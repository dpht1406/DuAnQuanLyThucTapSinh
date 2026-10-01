using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Notifications;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(NotificationFilterDto filter);
    Task<UnreadCountDto> GetUnreadCountAsync();
    Task<(bool Success, string? Error, bool NotFound)> MarkAsReadAsync(int id, int userId);
    Task MarkAllAsReadAsync(int userId);
}