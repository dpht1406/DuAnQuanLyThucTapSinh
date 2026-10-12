namespace StudentInternshipMgmt.Application.Features.Students;

public class ImportSettings
{
    public long MaxFileSizeBytes { get; set; } = 5_242_880;
    public int MaxRows { get; set; } = 5_000;
}
