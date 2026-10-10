using System.ComponentModel.DataAnnotations;
using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

public class RejectJobPositionDto
{
    [Required]
    [SafeString(1000, allowLineBreaks: true)]
    public string Note { get; set; } = string.Empty;
}
