namespace StudentInternshipMgmt.Application.Features.Dashboard;

public class RecentRejectionDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RejectedCompanyName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime RejectedAt { get; set; }
}