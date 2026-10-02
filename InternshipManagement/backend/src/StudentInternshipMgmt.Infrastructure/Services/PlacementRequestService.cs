using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<PlacementRequestService> _logger;

    public PlacementRequestService(AppDbContext db, ILogger<PlacementRequestService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public PlacementRequestService(AppDbContext db)
        : this(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<PlacementRequestService>.Instance)
    {
    }

    public async Task<(bool Success, string? Error, PlacementRequestDto? Data)> CreateRequestAsync(int studentId, CreatePlacementRequestDto dto)
    {
        (bool Success, string? Error, PlacementRequestDto? Data) Reject(string reason, string message)
        {
            _logger.LogWarning(
                "Placement request rejected: {Reason}; StudentId {StudentId}, CompanyId {CompanyId}, JobPositionId {JobPositionId}",
                reason, studentId, dto.CompanyId, dto.JobPositionId);
            return (false, message, null);
        }

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        if (student is null)
            return Reject("StudentNotFound", "Không tìm thấy sinh viên.");

        if (student.Status != StudentStatus.NoCompany)
            return Reject("StudentAlreadyHasCompany", "Chỉ có thể tạo yêu cầu khi chưa có doanh nghiệp.");

        var hasPending = await _db.PlacementRequests
            .AnyAsync(r => r.StudentId == studentId && r.Status == RequestStatus.Pending);
        if (hasPending)
            return Reject("PendingRequestExists", "Bạn đã có một yêu cầu đang chờ duyệt.");

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == dto.CompanyId);
        if (company is null)
            return Reject("CompanyNotFound", "Không tìm thấy doanh nghiệp.");

        JobPosition? jobPosition = null;
        if (dto.JobPositionId.HasValue)
        {
            jobPosition = await _db.JobPositions.FirstOrDefaultAsync(jp => jp.Id == dto.JobPositionId.Value);
            if (jobPosition is null)
                return Reject("JobPositionNotFound", "Không tìm thấy vị trí tuyển dụng.");

            if (jobPosition.CompanyId != dto.CompanyId)
                return Reject("JobPositionCompanyMismatch", "Vị trí tuyển dụng không thuộc doanh nghiệp đã chọn.");

            if (!jobPosition.IsOpen)
                return Reject("JobPositionClosed", "Vị trí này đã ngừng tuyển.");

            if (jobPosition.Deadline.HasValue && jobPosition.Deadline.Value.Date < DateTime.Today)
                return Reject("JobPositionExpired", "Vị trí này đã hết hạn nhận hồ sơ.");
        }

        var request = new PlacementRequest
        {
            StudentId = studentId,
            CompanyId = dto.CompanyId,
            JobPositionId = dto.JobPositionId,
            ApplicantFullName = CleanString(dto.ApplicantFullName),
            ApplicantEmail = CleanString(dto.ApplicantEmail)?.ToLowerInvariant(),
            ApplicantPhone = CleanString(dto.ApplicantPhone),
            ApplicantSchool = CleanString(dto.ApplicantSchool),
            ApplicantMajor = CleanString(dto.ApplicantMajor),
            CvUrl = CleanString(dto.CvUrl),
            CoverLetter = CleanString(dto.CoverLetter, preserveLineBreaks: true),
            Status = RequestStatus.Pending,
            Note = CleanString(dto.Note),
            CreatedAt = DateTime.UtcNow
        };

        _db.PlacementRequests.Add(request);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Placement request created: RequestId {RequestId}, StudentId {StudentId}, CompanyId {CompanyId}, JobPositionId {JobPositionId}",
            request.Id, studentId, request.CompanyId, request.JobPositionId);

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
            ApplicantFullName = request.ApplicantFullName,
            ApplicantEmail = request.ApplicantEmail,
            ApplicantPhone = request.ApplicantPhone,
            ApplicantSchool = request.ApplicantSchool,
            ApplicantMajor = request.ApplicantMajor,
            CvUrl = request.CvUrl,
            CoverLetter = request.CoverLetter,
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
                ApplicantFullName = r.ApplicantFullName,
                ApplicantEmail = r.ApplicantEmail,
                ApplicantPhone = r.ApplicantPhone,
                ApplicantSchool = r.ApplicantSchool,
                ApplicantMajor = r.ApplicantMajor,
                CvUrl = r.CvUrl,
                CoverLetter = r.CoverLetter,
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
                ApplicantFullName = r.ApplicantFullName,
                ApplicantEmail = r.ApplicantEmail,
                ApplicantPhone = r.ApplicantPhone,
                ApplicantSchool = r.ApplicantSchool,
                ApplicantMajor = r.ApplicantMajor,
                CvUrl = r.CvUrl,
                CoverLetter = r.CoverLetter,
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

    private static string? CleanString(string? value, bool preserveLineBreaks = false)
    {
        if (value is null)
            return null;

        return new string(value.Trim()
            .Where(character => !char.IsControl(character)
                || preserveLineBreaks && character is '\r' or '\n')
            .ToArray());
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
            ChangedBy = adminUserId,
            ChangedByType = ActorType.Admin
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
