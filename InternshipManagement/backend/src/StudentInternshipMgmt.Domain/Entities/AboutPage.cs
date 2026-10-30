using StudentInternshipMgmt.Domain.Common;

namespace StudentInternshipMgmt.Domain.Entities;

public class AboutPage : BaseEntity
{
    public string ContentJson { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
}
