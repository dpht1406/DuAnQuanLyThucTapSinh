using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;

    public PlacementRequestService(AppDbContext db, ILogger<PlacementRequestService> logger, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
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
            return Reject("StudentAlreadyHasCompany", "Sinh viên đã có doanh nghiệp hoặc đang được Admin xử lý; vui lòng liên hệ Admin.");

        var maxOpenApplications = _configuration.GetValue<int?>("MaxOpenApplicationsPerStudent") ?? 5;
        var openRequestCount = await _db.PlacementRequests.CountAsync(r =>
            r.StudentId == studentId
            && (r.Status == RequestStatus.Pending || r.Status == RequestStatus.InterviewInvited));
        if (openRequestCount >= maxOpenApplications)
            return Reject("OpenLimitReached", $"Bạn chỉ được có tối đa {maxOpenApplications} đơn đang mở cùng lúc.");

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

        var hasDuplicateOpenRequest = await _db.PlacementRequests.AnyAsync(r =>
            r.StudentId == studentId
            && r.CompanyId == dto.CompanyId
            && r.JobPositionId == dto.JobPositionId
            && (r.Status == RequestStatus.Pending || r.Status == RequestStatus.InterviewInvited));
        if (hasDuplicateOpenRequest)
            return Reject("DuplicateOpenRequest", "Bạn đã có đơn đang mở cho doanh nghiệp và vị trí này.");

        var createdAt = DateTime.UtcNow;
        var dueAt = createdAt.AddDays(_configuration.GetValue<int?>("ApplicationResponseDueDays") ?? 7);
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
            CreatedAt = createdAt,
            DueAt = dueAt
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
            DueAt = request.DueAt,
            InterviewAt = request.InterviewAt,
            InterviewLocation = request.InterviewLocation,
            InterviewNote = request.InterviewNote,
            ResultNote = request.ResultNote,
            StudentReportedInterviewAt = request.StudentReportedInterviewAt,
            StudentInterviewNote = request.StudentInterviewNote,
            ResultActorType = request.ResultActorType,
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
                DueAt = r.DueAt,
                InterviewAt = r.InterviewAt,
                InterviewLocation = r.InterviewLocation,
                InterviewNote = r.InterviewNote,
                ResultNote = r.ResultNote,
                StudentReportedInterviewAt = r.StudentReportedInterviewAt,
                StudentInterviewNote = r.StudentInterviewNote,
                ResultActorType = r.ResultActorType,
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
                DueAt = r.DueAt,
                InterviewAt = r.InterviewAt,
                InterviewLocation = r.InterviewLocation,
                InterviewNote = r.InterviewNote,
                ResultNote = r.ResultNote,
                StudentReportedInterviewAt = r.StudentReportedInterviewAt,
                StudentInterviewNote = r.StudentInterviewNote,
                ResultActorType = r.ResultActorType,
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

    public async Task<(bool Success, string? Error, bool NotFound)> ReportInterviewedAsync(
        int studentId,
        int requestId,
        ReportInterviewDto dto)
    {
        var request = await _db.PlacementRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.StudentId == studentId);

        if (request is null)
            return (false, "Không tìm thấy yêu cầu.", true);

        if (request.Status != RequestStatus.InterviewInvited)
            return (false, "Chỉ có thể báo đã phỏng vấn khi đơn ở trạng thái được mời phỏng vấn.", false);

        if (request.StudentReportedInterviewAt.HasValue)
            return (false, "Bạn đã báo đã phỏng vấn cho đơn này rồi.", false);

        request.StudentReportedInterviewAt = DateTime.UtcNow;
        request.StudentInterviewNote = CleanString(dto.Note, preserveLineBreaks: true);
        await _db.SaveChangesAsync();

        return (true, null, false);
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
