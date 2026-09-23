using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class PlacementRequestFilterDto : PaginationParams
{
    // Lọc theo trạng thái yêu cầu.
    public RequestStatus? Status { get; set; }
}
