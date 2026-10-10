using System.ComponentModel.DataAnnotations;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class AdminPlacementResultDto
{
    [Required]
    public AdminPlacementDecision Decision { get; set; }

    [Required]
    [SafeString(500, allowLineBreaks: true)]
    public string ConfirmationSource { get; set; } = string.Empty;

    [SafeString(1000, allowLineBreaks: true)]
    public string? Note { get; set; }
}
