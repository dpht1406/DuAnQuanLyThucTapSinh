using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Students;

public class AssignCompanyDto
{
    public int CompanyId { get; set; }
    public int? JobPositionId { get; set; }
    [SafeString(1000, allowLineBreaks: true)]
    public string? Note { get; set; }
}
