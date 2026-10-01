namespace StudentInternshipMgmt.Application.Features.JobPositions.Dtos;


public class JobPositionDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
}
