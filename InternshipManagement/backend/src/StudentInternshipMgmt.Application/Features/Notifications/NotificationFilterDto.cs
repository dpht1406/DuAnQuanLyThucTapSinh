using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Notifications;

public class NotificationFilterDto : PaginationParams
{
    public bool UnreadOnly { get; set; }
}