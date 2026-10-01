using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? StudentId { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? Reason { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}