using StudentInternshipMgmt.Domain.Common;

namespace StudentInternshipMgmt.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPosition { get; set; }

    public User? User { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<JobPosition> JobPositions { get; set; } = new List<JobPosition>();
}
