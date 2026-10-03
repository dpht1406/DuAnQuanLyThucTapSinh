using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Companies.Dtos;

public class CompanyFilterDto : PaginationParams
{
    // Tìm gần đúng theo Name (Contains, không phân biệt hoa/thường).
    [SafeString(200)]
    public string? Search { get; set; }

    // Lọc gần đúng (Contains), không phân biệt hoa/thường.
    [SafeString(150)]
    public string? Industry { get; set; }
}
