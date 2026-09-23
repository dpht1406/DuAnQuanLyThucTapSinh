namespace StudentInternshipMgmt.Application.Features.Dashboard;

public class DashboardSummaryDto
{
    public int TotalStudents { get; set; }
    public int WithCompanyCount { get; set; }
    public int WithoutCompanyCount { get; set; }
    public int CompletedCount { get; set; }
    public double CompletionRate { get; set; } // phần trăm, làm tròn 2 chữ số, 0 nếu TotalStudents = 0
}
