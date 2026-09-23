using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

public class JobPositionFilterDto : PaginationParams
{
    // Tìm gần đúng theo Title (Contains, không phân biệt hoa/thường).
    public string? Search { get; set; }

    // Lọc theo công ty.
    public int? CompanyId { get; set; }

    // Lọc theo trạng thái còn tuyển hay không.
    public bool? IsOpen { get; set; }
}
