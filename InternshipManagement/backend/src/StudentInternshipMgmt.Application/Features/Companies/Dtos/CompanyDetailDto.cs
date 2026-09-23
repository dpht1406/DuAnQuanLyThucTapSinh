namespace StudentInternshipMgmt.Application.Features.Companies.Dtos;

public class CompanyDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;

    public List<JobPositionDto> JobPositions { get; set; } = new();
}
