using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Companies.Dtos;

namespace StudentInternshipMgmt.Application.Features.Companies;

public interface ICompanyService
{
    Task<PagedResult<CompanyDto>> GetCompaniesAsync(CompanyFilterDto filter);

    Task<List<string>> GetDistinctIndustriesAsync();

    Task<CompanyDetailDto?> GetCompanyByIdAsync(int id);

    Task<(bool Success, string? Error, CompanyDto? Data)> CreateCompanyAsync(CreateCompanyDto dto);

    // NotFound tách riêng khỏi Error để controller trả đúng 404 (không tìm thấy)
    // hay 400/409 (lỗi validate / còn ràng buộc dữ liệu).
    Task<(bool Success, string? Error, bool NotFound)> UpdateCompanyAsync(int id, UpdateCompanyDto dto);

    Task<(bool Success, string? Error, bool NotFound)> DeleteCompanyAsync(int id);
}
