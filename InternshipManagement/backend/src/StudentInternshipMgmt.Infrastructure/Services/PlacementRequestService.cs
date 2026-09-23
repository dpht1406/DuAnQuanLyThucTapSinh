using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class PlacementRequestService : IPlacementRequestService
{
    private readonly AppDbContext _db;

    public PlacementRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string? Error, PlacementRequestDto? Data)> CreateRequestAsync(int studentId, CreatePlacementRequestDto dto)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        if (student is null)
            return (false, "Không tìm thấy sinh viên.", null);

        if (student.Status != StudentStatus.NoCompany)
            return (false, "Chỉ có thể tạo yêu cầu khi chưa có doanh nghiệp.", null);

        var hasPending = await _db.PlacementRequests
            .AnyAsync(r => r.StudentId == studentId && r.Status == RequestStatus.Pending);
        if (hasPending)
            return (false, "Bạn đã có một yêu cầu đang chờ duyệt.", null);

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == dto.CompanyId);
        if (company is null)
            return (false, "Không tìm thấy doanh nghiệp.", null);

        JobPosition? jobPosition = null;
        if (dto.JobPositionId.HasValue)
        {
            jobPosition = await _db.JobPositions.FirstOrDefaultAsync(jp => jp.Id == dto.JobPositionId.Value);
            if (jobPosition is null)
                return (false, "Không tìm thấy vị trí tuyển dụng.", null);

            if (jobPosition.CompanyId != dto.CompanyId)
                return (false, "Vị trí tuyển dụng không thuộc doanh nghiệp đã chọn.", null);

            if (!jobPosition.IsOpen)
                return (false, "Vị trí này đã ngừng tuyển.", null);
        }

        var request = new PlacementRequest
        {
            StudentId = studentId,
            CompanyId = dto.CompanyId,
            JobPositionId = dto.JobPositionId,
            Status = RequestStatus.Pending,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        _db.PlacementRequests.Add(request);
        await _db.SaveChangesAsync();

        return (true, null, new PlacementRequestDto
        {
            Id = request.Id,
            StudentId = student.Id,
            StudentCode = student.StudentCode,
            StudentFullName = student.FullName,
            CompanyId = company.Id,
            CompanyName = company.Name,
            JobPositionId = jobPosition?.Id,
            JobPositionTitle = jobPosition?.Title,
            Status = request.Status,
            Note = request.Note,
            RejectReason = request.RejectReason,
            CreatedAt = request.CreatedAt,
            ReviewedBy = request.ReviewedBy,
            ReviewedByUsername = null,
            ReviewedAt = request.ReviewedAt
        });
    }

    public async Task<PagedResult<PlacementRequestDto>> GetRequestsAsync(PlacementRequestFilterDto filter)
    {
        var query = _db.PlacementRequests
            .Include(r => r.Student)
            .Include(r => r.Company)
            .Include(r => r.JobPosition)
            .Include(r => r.ReviewedByUser)
            .AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);

        // Chiếu (Select) trực tiếp sang PlacementRequestDto ngay trong LINQ-to-Entities
        // để EF Core dịch được ra SQL — không dùng hàm map tay ở bước này.
        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PlacementRequestDto
            {
                Id = r.Id,
                StudentId = r.StudentId,
                StudentCode = r.Student.StudentCode,
                StudentFullName = r.Student.FullName,
                CompanyId = r.CompanyId,
                CompanyName = r.Company.Name,
                JobPositionId = r.JobPositionId,
                JobPositionTitle = r.JobPosition != null ? r.JobPosition.Title : null,
                Status = r.Status,
                Note = r.Note,
                RejectReason = r.RejectReason,
                CreatedAt = r.CreatedAt,
                ReviewedBy = r.ReviewedBy,
                ReviewedByUsername = r.ReviewedByUser != null ? r.ReviewedByUser.Username : null,
                ReviewedAt = r.ReviewedAt
            })
            .ToPagedResultAsync(filter);
    }

    public async Task<List<PlacementRequestDto>> GetMyRequestsAsync(int studentId)
    {
        // Dữ liệu ít (yêu cầu của riêng 1 sinh viên) — trả về List, không phân trang.
        return await _db.PlacementRequests
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PlacementRequestDto
            {
                Id = r.Id,
                StudentId = r.StudentId,
                StudentCode = r.Student.StudentCode,
                StudentFullName = r.Student.FullName,
                CompanyId = r.CompanyId,
                CompanyName = r.Company.Name,
                JobPositionId = r.JobPositionId,
                JobPositionTitle = r.JobPosition != null ? r.JobPosition.Title : null,
                Status = r.Status,
                Note = r.Note,
                RejectReason = r.RejectReason,
                CreatedAt = r.CreatedAt,
                ReviewedBy = r.ReviewedBy,
                ReviewedByUsername = r.ReviewedByUser != null ? r.ReviewedByUser.Username : null,
                ReviewedAt = r.ReviewedAt
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error, bool NotFound)> ApproveRequestAsync(int requestId, int adminUserId)
    {
        var request = await _db.PlacementRequests
            .Include(r => r.Student)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
            return (false, "Không tìm thấy yêu cầu.", true);

        if (request.Status != RequestStatus.Pending)
            return (false, "Yêu cầu này đã được xử lý trước đó.", false);

        if (request.Student.Status != StudentStatus.NoCompany)
            return (false, "Sinh viên không còn ở trạng thái chưa có doanh nghiệp.", false);

        var student = request.Student;
        student.CompanyId = request.CompanyId;
        student.JobPositionId = request.JobPositionId;
        student.Status = StudentStatus.Introduced;
        student.UpdatedAt = DateTime.UtcNow;

        _db.StatusHistories.Add(new StatusHistory
        {
            StudentId = student.Id,
            FromStatus = StudentStatus.NoCompany,
            ToStatus = StudentStatus.Introduced,
            CompanyId = request.CompanyId,
            Note = "Duyệt yêu cầu chọn doanh nghiệp.",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = adminUserId
        });

        request.Status = RequestStatus.Approved;
        request.ReviewedBy = adminUserId;
        request.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null, false);
    }

    public async Task<(bool Success, string? Error, bool NotFound)> RejectRequestAsync(int requestId, RejectPlacementRequestDto dto, int adminUserId)
    {
        var request = await _db.PlacementRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request is null)
            return (false, "Không tìm thấy yêu cầu.", true);

        if (request.Status != RequestStatus.Pending)
            return (false, "Yêu cầu này đã được xử lý trước đó.", false);

        request.Status = RequestStatus.Rejected;
        request.RejectReason = dto.RejectReason;
        request.ReviewedBy = adminUserId;
        request.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null, false);
    }
}
