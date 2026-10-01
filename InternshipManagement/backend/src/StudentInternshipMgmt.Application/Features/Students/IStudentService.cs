using StudentInternshipMgmt.Application.Common; // ApiResponse<T>, PagedResult<T> (giả định namespace)

namespace StudentInternshipMgmt.Application.Features.Students;

public interface IStudentService
{
    Task<PagedResult<StudentDto>> GetStudentsAsync(StudentFilterDto filter);

    Task<StudentDto?> GetStudentByIdAsync(int id);

    /// <returns>(thành công?, thông báo lỗi nếu có, dto vừa tạo)</returns>
    Task<(bool Success, string? Error, StudentDto? Data)> CreateStudentAsync(CreateStudentDto dto);

    Task<(bool Success, string? Error)> UpdateStudentAsync(int id, UpdateStudentDto dto);

    Task<(bool Success, string? Error)> DeleteStudentAsync(int id);

    Task<CreateAccountsResultDto> CreateAccountsAsync(List<int> studentIds);

    Task<CreateAccountsResultDto> CreateAccountsByFilterAsync(StudentFilterDto filter);

    Task<(bool Success, string? Error, ResetPasswordResultDto? Data)> ResetPasswordAsync(int studentId, int adminUserId);

    Task<ImportResultDto> ImportStudentsAsync(Stream fileStream, string fileName);

    Task<(bool Success, string? Error)> ChangeStatusAsync(int studentId, ChangeStatusDto dto, int changedByUserId, bool isAdmin);

    Task<List<StatusHistoryDto>?> GetStatusHistoryAsync(int studentId);

    Task<(bool Success, string? Error)> AssignCompanyDirectAsync(int studentId, AssignCompanyDto dto, int adminUserId);

    /// <summary>
    /// Xuất toàn bộ danh sách sinh viên khớp filter (không phân trang) ra file Excel (.xlsx) hoặc CSV (.csv).
    /// </summary>
    /// <param name="format">"csv" hoặc "xlsx" (không phân biệt hoa thường).</param>
    Task<(byte[] Content, string FileName, string ContentType)> ExportStudentsAsync(StudentFilterDto filter, string format);
}
