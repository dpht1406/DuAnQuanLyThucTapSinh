namespace StudentInternshipMgmt.Application.Features.Students;

public class ImportErrorDto
{
    public int RowNumber { get; set; }
    public string Reason { get; set; } = default!;
}

public class ImportResultDto
{
    public int SuccessCount { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = new();
}
