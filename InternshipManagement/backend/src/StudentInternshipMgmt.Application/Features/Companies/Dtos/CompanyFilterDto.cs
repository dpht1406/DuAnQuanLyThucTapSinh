using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Companies.Dtos;

public class CompanyFilterDto : PaginationParams
{
    // Tìm gần đúng theo Name (Contains, không phân biệt hoa/thường).
    public string? Search { get; set; }

    // Lọc gần đúng (Contains), không phân biệt hoa/thường.
    public string? Industry { get; set; }
}
