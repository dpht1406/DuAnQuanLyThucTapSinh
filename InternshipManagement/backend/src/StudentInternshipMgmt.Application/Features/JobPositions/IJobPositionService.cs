using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

namespace StudentInternshipMgmt.Application.Features.JobPositions;

public interface IJobPositionService
{
    // Danh sách cho User (chỉ đọc) — không có AcceptedCount.
    Task<PagedResult<JobPositionDto>> GetJobPositionsAsync(JobPositionFilterDto filter);

    // Danh sách cho Admin — có AcceptedCount.
    Task<PagedResult<JobPositionAdminDto>> GetJobPositionsAdminAsync(JobPositionFilterDto filter);

    Task<JobPositionDto?> GetJobPositionByIdAsync(int id);

    Task<JobPositionAdminDto?> GetJobPositionByIdAdminAsync(int id);

    Task<PagedResult<JobPositionAdminDto>> GetJobPositionsForCompanyAsync(int companyId, JobPositionFilterDto filter);

    Task<JobPositionAdminDto?> GetJobPositionByIdForCompanyAsync(int companyId, int id);

    Task<(bool Success, string? Error, JobPositionDto? Data)> CreateJobPositionAsync(CreateJobPositionDto dto);

    Task<(bool Success, string? Error, JobPositionAdminDto? Data)> CreateJobPositionForCompanyAsync(
        int companyId, CreateCompanyJobPositionDto dto);

    // NotFound tách riêng khỏi Error để controller trả đúng 404 (không tìm thấy)
    // hay 400 (lỗi validate / vi phạm ràng buộc Quantity < AcceptedCount).
    Task<(bool Success, string? Error, bool NotFound)> UpdateJobPositionAsync(int id, UpdateJobPositionDto dto);

    Task<(bool Success, string? Error, bool NotFound, JobPositionAdminDto? Data)> UpdateJobPositionForCompanyAsync(
        int companyId, int id, UpdateCompanyJobPositionDto dto);

    // Chặn xóa cứng nếu còn Student (chưa xóa mềm) gắn với vị trí này.
    Task<(bool Success, string? Error, bool NotFound)> DeleteJobPositionAsync(int id);

    // isOpen = null -> đảo giá trị hiện tại; có giá trị -> set đúng giá trị đó.
    Task<(bool Success, string? Error, bool NotFound)> ToggleOpenAsync(int id, bool? isOpen);

    Task<(bool Success, string? Error, bool NotFound)> ToggleOpenForCompanyAsync(int companyId, int id, bool? isOpen);

    Task<(bool Success, string? Error, bool NotFound)> SubmitForApprovalAsync(int companyId, int id);

    Task<(bool Success, string? Error, bool NotFound)> ApproveJobPositionAsync(int id);

    Task<(bool Success, string? Error, bool NotFound)> RejectJobPositionAsync(int id, string note);

    Task<(bool Success, string? Error, bool NotFound)> DeleteJobPositionForCompanyAsync(int companyId, int id);
}
