namespace StudentInternshipMgmt.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<List<NeedAssignmentDto>> GetNeedAssignmentListAsync();
    Task<List<RecentRejectionDto>> GetRecentRejectionsAsync();
}
