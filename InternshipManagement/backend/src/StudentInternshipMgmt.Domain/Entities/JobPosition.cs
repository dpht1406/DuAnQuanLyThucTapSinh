using StudentInternshipMgmt.Domain.Common;

namespace StudentInternshipMgmt.Domain.Entities;

public class JobPosition : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOpen { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
