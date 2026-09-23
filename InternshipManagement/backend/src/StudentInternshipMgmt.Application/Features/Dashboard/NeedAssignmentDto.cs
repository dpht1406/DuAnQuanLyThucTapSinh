namespace StudentInternshipMgmt.Application.Features.Dashboard;

public class NeedAssignmentDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public DateTime AccountCreatedAt { get; set; }
    public int DaysSinceCreated { get; set; }
}
