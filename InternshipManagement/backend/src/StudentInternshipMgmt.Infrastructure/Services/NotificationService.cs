using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Notifications;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(NotificationFilterDto filter)
    {
        var query = _db.Notifications.AsNoTracking().AsQueryable();
        if (filter.UnreadOnly)
            query = query.Where(notification => !notification.IsRead);

        return await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Select(notification => new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                StudentId = notification.StudentId,
                CompanyId = notification.CompanyId,
                CompanyName = notification.CompanyName,
                Reason = notification.Reason,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt
            })
            .ToPagedResultAsync(filter);
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync()
    {
        return new UnreadCountDto
        {
            UnreadCount = await _db.Notifications.CountAsync(notification => !notification.IsRead)
        };
    }

    public async Task<(bool Success, string? Error, bool NotFound)> MarkAsReadAsync(int id, int userId)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(item => item.Id == id);
        if (notification is null)
            return (false, "Không tìm thấy thông báo.", true);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.ReadBy = userId;
            await _db.SaveChangesAsync();
        }

        return (true, null, false);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _db.Notifications
            .Where(notification => !notification.IsRead)
            .ToListAsync();
        var readAt = DateTime.UtcNow;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = readAt;
            notification.ReadBy = userId;
        }

        if (unreadNotifications.Count > 0)
            await _db.SaveChangesAsync();
    }
}