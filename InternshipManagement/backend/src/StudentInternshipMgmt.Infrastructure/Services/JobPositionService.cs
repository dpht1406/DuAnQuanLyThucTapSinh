using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.JobPositions;
using StudentInternshipMgmt.Application.Features.JobPositions.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class JobPositionService : IJobPositionService
{
    // Khớp đúng HasMaxLength trong JobPositionConfiguration — tránh DbUpdateException khi
    // dữ liệu vượt quá cột NVARCHAR, đồng thời trả lỗi rõ ràng thay vì lỗi SQL thô.
    private const int TitleMaxLength = 200;
    private const int DescriptionMaxLength = 2000;

    // Các Status được tính là "đã nhận" vào vị trí này.
    private static readonly StudentStatus[] AcceptedStatuses =
    {
        StudentStatus.Accepted,
        StudentStatus.Interning,
        StudentStatus.Completed
    };

    private readonly AppDbContext _db;

    public JobPositionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<JobPositionDto>> GetJobPositionsAsync(JobPositionFilterDto filter)
    {
        var query = BuildFilteredQuery(filter);
        var today = DateTime.Today;

        return await ApplyOrdering(query, filter.Sort, today)
            .Select(jp => new JobPositionDto
            {
                Id = jp.Id,
                CompanyId = jp.CompanyId,
                CompanyName = jp.Company.Name,
                Title = jp.Title,
                Department = jp.Department,
                Location = jp.Location,
                DisplayLocation = jp.Location != null && jp.Location.Trim() != "" ? jp.Location : jp.Company.Address,
                Deadline = jp.Deadline,
                IsExpired = jp.Deadline.HasValue && jp.Deadline.Value < today,
                Quantity = jp.Quantity,
                Description = jp.Description,
                IsOpen = jp.IsOpen
            })
            .ToPagedResultAsync(filter);
    }

    public async Task<PagedResult<JobPositionAdminDto>> GetJobPositionsAdminAsync(JobPositionFilterDto filter)
    {
        var query = BuildFilteredQuery(filter);
        var today = DateTime.Today;

        return await ApplyOrdering(query, filter.Sort, today)
            .Select(jp => new JobPositionAdminDto
            {
                Id = jp.Id,
                CompanyId = jp.CompanyId,
                CompanyName = jp.Company.Name,
                Title = jp.Title,
                Department = jp.Department,
                Location = jp.Location,
                DisplayLocation = jp.Location != null && jp.Location.Trim() != "" ? jp.Location : jp.Company.Address,
                Deadline = jp.Deadline,
                IsExpired = jp.Deadline.HasValue && jp.Deadline.Value < today,
                Quantity = jp.Quantity,
                Description = jp.Description,
                IsOpen = jp.IsOpen,
                // Students đã có global query filter (!IsDeleted) cấu hình ở
                // StudentConfiguration, nên không cần lặp lại điều kiện đó ở đây.
                AcceptedCount = _db.Students.Count(s =>
                    s.JobPositionId == jp.Id &&
                    (s.Status == StudentStatus.Accepted ||
                     s.Status == StudentStatus.Interning ||
                     s.Status == StudentStatus.Completed))
            })
            .ToPagedResultAsync(filter);
    }

    public async Task<JobPositionDto?> GetJobPositionByIdAsync(int id)
    {
        var today = DateTime.Today;
        return await _db.JobPositions
            .Where(jp => jp.Id == id)
            .Select(jp => new JobPositionDto
            {
                Id = jp.Id,
                CompanyId = jp.CompanyId,
                CompanyName = jp.Company.Name,
                Title = jp.Title,
                Department = jp.Department,
                Location = jp.Location,
                DisplayLocation = jp.Location != null && jp.Location.Trim() != "" ? jp.Location : jp.Company.Address,
                Deadline = jp.Deadline,
                IsExpired = jp.Deadline.HasValue && jp.Deadline.Value < today,
                Quantity = jp.Quantity,
                Description = jp.Description,
                IsOpen = jp.IsOpen
            })
            .FirstOrDefaultAsync();
    }

    public async Task<JobPositionAdminDto?> GetJobPositionByIdAdminAsync(int id)
    {
        var today = DateTime.Today;
        return await _db.JobPositions
            .Where(jp => jp.Id == id)
            .Select(jp => new JobPositionAdminDto
            {
                Id = jp.Id,
                CompanyId = jp.CompanyId,
                CompanyName = jp.Company.Name,
                Title = jp.Title,
                Department = jp.Department,
                Location = jp.Location,
                DisplayLocation = jp.Location != null && jp.Location.Trim() != "" ? jp.Location : jp.Company.Address,
                Deadline = jp.Deadline,
                IsExpired = jp.Deadline.HasValue && jp.Deadline.Value < today,
                Quantity = jp.Quantity,
                Description = jp.Description,
                IsOpen = jp.IsOpen,
                AcceptedCount = _db.Students.Count(s =>
                    s.JobPositionId == jp.Id &&
                    (s.Status == StudentStatus.Accepted ||
                     s.Status == StudentStatus.Interning ||
                     s.Status == StudentStatus.Completed))
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error, JobPositionDto? Data)> CreateJobPositionAsync(CreateJobPositionDto dto)
    {
        var validationError = ValidateFields(dto.Title, dto.Department, dto.Location, dto.Deadline, dto.Quantity, dto.Description, true);
        if (validationError is not null)
            return (false, validationError, null);

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == dto.CompanyId);
        if (company is null)
            return (false, "Không tìm thấy công ty.", null);

        var jobPosition = new JobPosition
        {
            CompanyId = dto.CompanyId,
            Title = dto.Title.Trim(),
            Department = (dto.Department ?? string.Empty).Trim(),
            Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
            Deadline = dto.Deadline?.Date,
            Quantity = dto.Quantity,
            Description = dto.Description.Trim(),
            IsOpen = dto.IsOpen
        };

        _db.JobPositions.Add(jobPosition);
        await _db.SaveChangesAsync();

        return (true, null, new JobPositionDto
        {
            Id = jobPosition.Id,
            CompanyId = jobPosition.CompanyId,
            CompanyName = company.Name,
            Title = jobPosition.Title,
            Department = jobPosition.Department,
            Location = jobPosition.Location,
            DisplayLocation = string.IsNullOrWhiteSpace(jobPosition.Location) ? company.Address : jobPosition.Location,
            Deadline = jobPosition.Deadline,
            IsExpired = jobPosition.Deadline.HasValue && jobPosition.Deadline.Value.Date < DateTime.Today,
            Quantity = jobPosition.Quantity,
            Description = jobPosition.Description,
            IsOpen = jobPosition.IsOpen
        });
    }

    public async Task<(bool Success, string? Error, bool NotFound)> UpdateJobPositionAsync(int id, UpdateJobPositionDto dto)
    {
        var jobPosition = await _db.JobPositions.FirstOrDefaultAsync(jp => jp.Id == id);
        if (jobPosition is null)
            return (false, "Không tìm thấy vị trí tuyển dụng.", true);

        var validationError = ValidateFields(dto.Title, dto.Department, dto.Location, dto.Deadline, dto.Quantity, dto.Description, false);
        if (validationError is not null)
            return (false, validationError, false);

        var acceptedCount = await CountAcceptedAsync(id);
        if (dto.Quantity < acceptedCount)
            return (false,
                $"Không thể giảm số lượng tuyển (Quantity) xuống dưới số lượng đã nhận hiện tại ({acceptedCount}).",
                false);

        jobPosition.Title = dto.Title.Trim();
        jobPosition.Department = (dto.Department ?? string.Empty).Trim();
        jobPosition.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
        jobPosition.Deadline = dto.Deadline?.Date;
        jobPosition.Quantity = dto.Quantity;
        jobPosition.Description = dto.Description.Trim();
        jobPosition.IsOpen = dto.IsOpen;

        await _db.SaveChangesAsync();
        return (true, null, false);
    }

    public async Task<(bool Success, string? Error, bool NotFound)> DeleteJobPositionAsync(int id)
    {
        var jobPosition = await _db.JobPositions.FirstOrDefaultAsync(jp => jp.Id == id);
        if (jobPosition is null)
            return (false, "Không tìm thấy vị trí tuyển dụng.", true);

        // Check chính theo đúng yêu cầu: còn Student nào (chưa xóa mềm, bất kể Status)
        // có JobPositionId = id này. Students đã có global query filter (!IsDeleted)
        // ở StudentConfiguration nên không cần lặp lại điều kiện đó ở đây.
        var hasActiveStudents = await _db.Students.AnyAsync(s => s.JobPositionId == id);
        if (hasActiveStudents)
            return (false,
                "Không thể xóa vì vẫn còn sinh viên đang gắn với vị trí tuyển dụng này.",
                false);

        _db.JobPositions.Remove(jobPosition);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Lưu ý: PlacementRequest.JobPositionId cũng là FK OnDelete(Restrict)
            // (xem PlacementRequestConfiguration). Đề bài yêu cầu không đụng tới
            // logic PlacementRequest, nhưng nếu còn PlacementRequest tham chiếu vị
            // trí này, SQL Server vẫn chặn ở tầng DB và ném DbUpdateException — bắt
            // lại ở đây để không lộ lỗi 500 thô ra ngoài, giữ đúng tinh thần "trả
            // lỗi rõ ràng" của yêu cầu 2c (cùng kỹ thuật đã dùng ở CompanyService).
            return (false,
                "Không thể xóa vì vị trí tuyển dụng này vẫn đang được tham chiếu bởi dữ liệu khác.",
                false);
        }

        return (true, null, false);
    }

    public async Task<(bool Success, string? Error, bool NotFound)> ToggleOpenAsync(int id, bool? isOpen)
    {
        var jobPosition = await _db.JobPositions.FirstOrDefaultAsync(jp => jp.Id == id);
        if (jobPosition is null)
            return (false, "Không tìm thấy vị trí tuyển dụng.", true);

        // Cho phép đổi bất cứ lúc nào, kể cả khi còn Student đang gắn vào vị trí này.
        jobPosition.IsOpen = isOpen ?? !jobPosition.IsOpen;

        await _db.SaveChangesAsync();
        return (true, null, false);
    }

    // ---------- Helpers ----------

    private IQueryable<JobPosition> BuildFilteredQuery(JobPositionFilterDto filter)
    {
        var query = _db.JobPositions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(jp =>
                jp.Title.ToLower().Contains(search) ||
                jp.Company.Name.ToLower().Contains(search) ||
                jp.Department.ToLower().Contains(search));
        }

        if (filter.CompanyId.HasValue)
            query = query.Where(jp => jp.CompanyId == filter.CompanyId.Value);

        if (filter.IsOpen.HasValue)
            query = query.Where(jp => jp.IsOpen == filter.IsOpen.Value);

        var today = DateTime.Today;
        if (string.Equals(filter.Availability?.Trim(), "open", StringComparison.OrdinalIgnoreCase))
            query = query.Where(jp => jp.IsOpen && (!jp.Deadline.HasValue || jp.Deadline.Value >= today));
        else if (string.Equals(filter.Availability?.Trim(), "closed", StringComparison.OrdinalIgnoreCase))
            query = query.Where(jp => !jp.IsOpen || (jp.Deadline.HasValue && jp.Deadline.Value < today));

        return query;
    }

    private static IOrderedQueryable<JobPosition> ApplyOrdering(IQueryable<JobPosition> query, string? sort, DateTime today)
    {
        if (string.Equals(sort?.Trim(), "recommended", StringComparison.OrdinalIgnoreCase))
        {
            return query
                .OrderByDescending(jp => jp.IsOpen && (!jp.Deadline.HasValue || jp.Deadline.Value >= today))
                .ThenBy(jp => jp.Deadline == null)
                .ThenBy(jp => jp.Deadline)
                .ThenBy(jp => jp.Title);
        }

        return query.OrderBy(jp => jp.Title);
    }

    private Task<int> CountAcceptedAsync(int jobPositionId)
    {
        // Students đã có global query filter (!IsDeleted) — không lặp lại ở đây.
        return _db.Students.CountAsync(s =>
            s.JobPositionId == jobPositionId &&
            AcceptedStatuses.Contains(s.Status));
    }

    private static string? ValidateFields(string title, string department, string? location, DateTime? deadline, int quantity, string description, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Tiêu đề (Title) là bắt buộc.";
        if (title.Trim().Length > TitleMaxLength)
            return $"Tiêu đề (Title) không được quá {TitleMaxLength} ký tự.";

        if ((department ?? string.Empty).Trim().Length > 100)
            return "Phòng ban không được quá 100 ký tự.";

        if ((location ?? string.Empty).Trim().Length > 300)
            return "Địa điểm không được quá 300 ký tự.";

        if (isCreate && deadline.HasValue && deadline.Value.Date < DateTime.Today)
            return "Hạn nộp hồ sơ không được trước ngày hôm nay.";

        if (quantity <= 0)
            return "Số lượng tuyển (Quantity) phải lớn hơn 0.";

        if (description.Trim().Length > DescriptionMaxLength)
            return $"Mô tả (Description) không được quá {DescriptionMaxLength} ký tự.";

        return null;
    }
}
