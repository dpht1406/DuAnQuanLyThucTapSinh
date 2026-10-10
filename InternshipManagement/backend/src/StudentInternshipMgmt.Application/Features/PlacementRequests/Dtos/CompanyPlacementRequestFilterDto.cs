using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

/// <summary>Filters and paginates the current company's placement requests.</summary>
public class CompanyPlacementRequestFilterDto : PaginationParams
{
    public RequestStatus? Status { get; set; }
    public int? JobPositionId { get; set; }
}
