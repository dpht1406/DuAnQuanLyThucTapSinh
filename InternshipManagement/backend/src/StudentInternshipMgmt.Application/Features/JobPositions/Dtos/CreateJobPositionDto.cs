namespace StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

// Không dùng [Required]/[MaxLength] — validate thủ công trong JobPositionService,
// đi theo đúng pattern CreateCompanyDto/CreateStudentDto đã dùng.
public class CreateJobPositionDto
{
    public int CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOpen { get; set; } = true;
}
