using StudentInternshipMgmt.Domain.Common;

namespace StudentInternshipMgmt.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;

    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<JobPosition> JobPositions { get; set; } = new List<JobPosition>();
}
