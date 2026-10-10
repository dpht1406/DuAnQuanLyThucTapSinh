using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly IPlacementAcceptanceService _placementAcceptanceService;

    public PlacementRequestService(AppDbContext db, ILogger<PlacementRequestService> logger, IConfiguration configuration)
        : this(db, logger, configuration, new PlacementAcceptanceService(
            db, NullLogger<PlacementAcceptanceService>.Instance))
    {
    }

    public PlacementRequestService(
        AppDbContext db,
        ILogger<PlacementRequestService> logger,
        IConfiguration configuration,
        IPlacementAcceptanceService placementAcceptanceService)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
        _placementAcceptanceService = placementAcceptanceService;
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
        if (maxOpenApplications <= 0)
        {
            _logger.LogWarning(
                "Invalid configuration {ConfigurationKey} value {ConfigurationValue}; using default {DefaultValue}",
                "MaxOpenApplicationsPerStudent", maxOpenApplications, 5);
            maxOpenApplications = 5;
        }

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

            if (jobPosition.ApprovalStatus != JobPositionApprovalStatus.Approved)
                return Reject("JobPositionNotApproved", "Vị trí này chưa được duyệt.");

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
        var responseDueDays = _configuration.GetValue<int?>("ApplicationResponseDueDays") ?? 7;
        if (responseDueDays <= 0)
        {
            _logger.LogWarning(
                "Invalid configuration {ConfigurationKey} value {ConfigurationValue}; using default {DefaultValue}",
                "ApplicationResponseDueDays", responseDueDays, 7);
            responseDueDays = 7;
        }
        var dueAt = createdAt.AddDays(responseDueDays);
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

    public async Task<PagedResult<CompanyPlacementRequestDto>> GetCompanyRequestsAsync(
        int companyId,
        CompanyPlacementRequestFilterDto filter)
    {
        var query = _db.PlacementRequests
            .Where(request => request.CompanyId == companyId);

        if (filter.Status.HasValue)
            query = query.Where(request => request.Status == filter.Status.Value);

        if (filter.JobPositionId.HasValue)
            query = query.Where(request => request.JobPositionId == filter.JobPositionId.Value);

        return await query
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new CompanyPlacementRequestDto
            {
                Id = request.Id,
                JobPositionId = request.JobPositionId,
                JobPositionTitle = request.JobPosition != null ? request.JobPosition.Title : null,
                StudentFullName = request.Student.FullName,
                Major = request.Student.Major,
                ClassName = request.Student.ClassName,
                StudentStatus = request.Student.Status,
                ApplicantFullName = request.ApplicantFullName,
                ApplicantEmail = request.ApplicantEmail,
                ApplicantPhone = request.ApplicantPhone,
                ApplicantSchool = request.ApplicantSchool,
                ApplicantMajor = request.ApplicantMajor,
                CvUrl = request.CvUrl,
                CoverLetter = request.CoverLetter,
                Note = request.Note,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                DueAt = request.DueAt,
                InterviewAt = request.InterviewAt,
                InterviewLocation = request.InterviewLocation,
                InterviewNote = request.InterviewNote,
                RejectReason = request.RejectReason,
                ResultNote = request.ResultNote,
                ResultActorType = request.ResultActorType
            })
            .ToPagedResultAsync(filter);
    }

    public Task<CompanyPlacementRequestDto?> GetCompanyRequestByIdAsync(int companyId, int requestId)
    {
        return _db.PlacementRequests
            .Where(request => request.Id == requestId
                && request.CompanyId == companyId)
            .Select(request => new CompanyPlacementRequestDto
            {
                Id = request.Id,
                JobPositionId = request.JobPositionId,
                JobPositionTitle = request.JobPosition != null ? request.JobPosition.Title : null,
                StudentFullName = request.Student.FullName,
                Major = request.Student.Major,
                ClassName = request.Student.ClassName,
                StudentStatus = request.Student.Status,
                ApplicantFullName = request.ApplicantFullName,
                ApplicantEmail = request.ApplicantEmail,
                ApplicantPhone = request.ApplicantPhone,
                ApplicantSchool = request.ApplicantSchool,
                ApplicantMajor = request.ApplicantMajor,
                CvUrl = request.CvUrl,
                CoverLetter = request.CoverLetter,
                Note = request.Note,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                DueAt = request.DueAt,
                InterviewAt = request.InterviewAt,
                InterviewLocation = request.InterviewLocation,
                InterviewNote = request.InterviewNote,
                RejectReason = request.RejectReason,
                ResultNote = request.ResultNote,
                ResultActorType = request.ResultActorType
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error, bool NotFound)> InviteCompanyRequestToInterviewAsync(
        int companyId,
        int requestId,
        InviteInterviewDto dto,
        int companyUserId)
    {
        var request = await _db.PlacementRequests.FirstOrDefaultAsync(item =>
            item.Id == requestId
            && item.CompanyId == companyId);

        if (request is null)
        {
            LogCompanyRequestAction("InviteInterview", companyId, requestId, companyUserId, false, "NotFound");
            return (false, "Không tìm thấy đơn ứng tuyển.", true);
        }

        if (request.Status != RequestStatus.Pending)
        {
            var error = request.Status is RequestStatus.Rejected or RequestStatus.Accepted or RequestStatus.Expired
                ? "Đơn này đã được xử lý."
                : "Chỉ có thể mời phỏng vấn khi đơn đang chờ xử lý.";
            LogCompanyRequestAction("InviteInterview", companyId, requestId, companyUserId, false, "InvalidStatus");
            return (false, error, false);
        }

        request.Status = RequestStatus.InterviewInvited;
        request.InterviewAt = NormalizeInterviewAtUtc(dto.InterviewAt);
        request.InterviewLocation = CleanString(dto.InterviewLocation);
        request.InterviewNote = CleanString(dto.InterviewNote, preserveLineBreaks: true);
        request.ReviewedBy = companyUserId;
        request.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        LogCompanyRequestAction("InviteInterview", companyId, requestId, companyUserId, true, "Updated");
        return (true, null, false);
    }

    public async Task<(bool Success, string? Error, bool NotFound)> RejectCompanyRequestAsync(
        int companyId,
        int requestId,
        CompanyRejectPlacementRequestDto dto,
        int companyUserId)
    {
        var request = await _db.PlacementRequests.FirstOrDefaultAsync(item =>
            item.Id == requestId
            && item.CompanyId == companyId);

        if (request is null)
        {
            LogCompanyRequestAction("Reject", companyId, requestId, companyUserId, false, "NotFound");
            return (false, "Không tìm thấy đơn ứng tuyển.", true);
        }

        if (request.Status is not (RequestStatus.Pending or RequestStatus.InterviewInvited))
        {
            var error = request.Status is RequestStatus.Rejected or RequestStatus.Accepted or RequestStatus.Expired
                ? "Đơn này đã được xử lý."
                : "Không thể từ chối đơn ở trạng thái hiện tại.";
            LogCompanyRequestAction("Reject", companyId, requestId, companyUserId, false, "InvalidStatus");
            return (false, error, false);
        }

        request.Status = RequestStatus.Rejected;
        request.RejectReason = CleanString(dto.Reason, preserveLineBreaks: true);
        request.ResultActorType = ActorType.Company;
        request.ReviewedBy = companyUserId;
        request.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        LogCompanyRequestAction("Reject", companyId, requestId, companyUserId, true, "Updated");
        return (true, null, false);
    }

    public async Task<PlacementRequestOperationResult> RecordCompanyResultAsync(
        int companyId,
        int requestId,
        CompanyPlacementResultDto dto,
        int companyUserId)
    {
        if (dto.IsPassed)
        {
            var accepted = await _placementAcceptanceService.AcceptAsync(
                requestId,
                companyId,
                ActorType.Company,
                companyUserId,
                dto.Note,
                confirmationSource: null);

            LogCompanyRequestAction(
                "RecordResult",
                companyId,
                requestId,
                companyUserId,
                accepted.Status == PlacementRequestOperationStatus.Success,
                accepted.Status.ToString());
            return accepted;
        }

        var request = await _db.PlacementRequests.FirstOrDefaultAsync(item =>
            item.Id == requestId
            && item.CompanyId == companyId);

        if (request is null)
        {
            LogCompanyRequestAction("RecordResult", companyId, requestId, companyUserId, false, "NotFound");
            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.NotFound,
                "Không tìm thấy đơn ứng tuyển.");
        }

        if (request.Status is not (RequestStatus.Pending or RequestStatus.InterviewInvited))
        {
            LogCompanyRequestAction("RecordResult", companyId, requestId, companyUserId, false, "InvalidStatus");
            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.BusinessError,
                "Đơn này đã được xử lý.");
        }

        if (string.IsNullOrWhiteSpace(dto.Note))
        {
            LogCompanyRequestAction("RecordResult", companyId, requestId, companyUserId, false, "MissingNote");
            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.BusinessError,
                "Ghi chú là bắt buộc khi sinh viên không đạt.");
        }

        request.Status = RequestStatus.Rejected;
        request.ResultNote = CleanString(dto.Note, preserveLineBreaks: true);
        request.ResultActorType = ActorType.Company;
        request.ReviewedBy = companyUserId;
        request.ReviewedAt = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            _logger.LogError(
                exception,
                "Company placement result could not be saved: RequestId {RequestId}, CompanyId {CompanyId}",
                requestId,
                companyId);
            LogCompanyRequestAction("RecordResult", companyId, requestId, companyUserId, false, "PersistenceFailure");
            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.Conflict,
                "Không thể hoàn tất, vui lòng thử lại.");
        }

        LogCompanyRequestAction("RecordResult", companyId, requestId, companyUserId, true, "Rejected");
        return new PlacementRequestOperationResult(PlacementRequestOperationStatus.Success);
    }

    private void LogCompanyRequestAction(
        string action,
        int companyId,
        int requestId,
        int companyUserId,
        bool success,
        string outcome)
    {
        if (success)
        {
            _logger.LogInformation(
                "Company placement request action {Action} completed: RequestId {RequestId}, CompanyId {CompanyId}, UserId {UserId}, Outcome {Outcome}",
                action, requestId, companyId, companyUserId, outcome);
            return;
        }

        _logger.LogWarning(
            "Company placement request action {Action} rejected: RequestId {RequestId}, CompanyId {CompanyId}, UserId {UserId}, Outcome {Outcome}",
            action, requestId, companyId, companyUserId, outcome);
    }

    private static DateTime NormalizeInterviewAtUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

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

    public async Task<PagedResult<PlacementRequestDto>> GetPendingConfirmationAsync(PlacementRequestFilterDto filter)
    {
        var query = _db.PlacementRequests
            .AsNoTracking()
            .Where(request => request.Status == RequestStatus.InterviewInvited
                && request.StudentReportedInterviewAt.HasValue
                && request.StudentReportDismissedAt == null);

        return await query
            .OrderByDescending(request => request.StudentReportedInterviewAt ?? request.CreatedAt)
            .Select(request => new PlacementRequestDto
            {
                Id = request.Id,
                StudentId = request.StudentId,
                StudentCode = request.Student.StudentCode,
                StudentFullName = request.Student.FullName,
                CompanyId = request.CompanyId,
                CompanyName = request.Company.Name,
                JobPositionId = request.JobPositionId,
                JobPositionTitle = request.JobPosition != null ? request.JobPosition.Title : null,
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
                ReviewedByUsername = request.ReviewedByUser != null ? request.ReviewedByUser.Username : null,
                ReviewedAt = request.ReviewedAt
            })
            .ToPagedResultAsync(filter);
    }

    public async Task<PlacementRequestOperationResult> AdminConfirmResultAsync(
        int requestId,
        int adminUserId,
        AdminPlacementResultDto dto)
    {
        var request = await _db.PlacementRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
            return new PlacementRequestOperationResult(PlacementRequestOperationStatus.NotFound, "Không tìm thấy đơn ứng tuyển.");

        if (request.Status != RequestStatus.InterviewInvited || !request.StudentReportedInterviewAt.HasValue)
            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.BusinessError,
                "Đơn này không còn trong trạng thái chờ Admin xác nhận báo cáo phỏng vấn.");

        if (request.StudentReportDismissedAt.HasValue)
            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.BusinessError,
                "Báo cáo phỏng vấn này đã được xử lý.");

        var confirmationSource = dto.ConfirmationSource?.Trim();
        if (string.IsNullOrWhiteSpace(confirmationSource) || confirmationSource.Length > 500)
            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.BusinessError,
                "Nguồn xác nhận là bắt buộc và tối đa 500 ký tự.");

        if (dto.Note is not null && dto.Note.Trim().Length > 1000)
            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.BusinessError,
                "Ghi chú không được vượt quá 1000 ký tự.");

        switch (dto.Decision)
        {
            case AdminPlacementDecision.Passed:
                return await _placementAcceptanceService.AcceptAsync(
                    requestId,
                    null,
                    ActorType.Admin,
                    adminUserId,
                    dto.Note,
                    confirmationSource);

            case AdminPlacementDecision.Failed:
                if (string.IsNullOrWhiteSpace(dto.Note))
                    return new PlacementRequestOperationResult(
                        PlacementRequestOperationStatus.BusinessError,
                        "Ghi chú là bắt buộc khi Admin xác nhận sinh viên không đạt.");

                using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (_db.Database.IsRelational())
                        {
                            await _db.Database.ExecuteSqlInterpolatedAsync(
                                $"UPDATE [PlacementRequests] SET [Status] = [Status] WHERE [Id] = {requestId}");
                        }

                        var lockedRequest = await _db.PlacementRequests
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(item => item.Id == requestId);

                        if (lockedRequest is not null)
                            await _db.Entry(lockedRequest).ReloadAsync();

                        if (lockedRequest is null
                            || lockedRequest.Status != RequestStatus.InterviewInvited
                            || !lockedRequest.StudentReportedInterviewAt.HasValue
                            || lockedRequest.StudentReportDismissedAt.HasValue)
                        {
                            await transaction.RollbackAsync();
                            return new PlacementRequestOperationResult(
                                PlacementRequestOperationStatus.BusinessError,
                                "Báo cáo phỏng vấn này đã được xử lý.");
                        }

                        lockedRequest.Status = RequestStatus.Rejected;
                        lockedRequest.ResultNote = CleanString(dto.Note, preserveLineBreaks: true);
                        lockedRequest.ResultActorType = ActorType.Admin;
                        lockedRequest.ReviewedBy = adminUserId;
                        lockedRequest.ReviewedAt = DateTime.UtcNow;
                        lockedRequest.ConfirmationSource = confirmationSource;
                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return new PlacementRequestOperationResult(PlacementRequestOperationStatus.Success);
                    }
                    catch (Exception exception) when (exception is DbUpdateException or DbException)
                    {
                        await transaction.RollbackAsync();
                        return new PlacementRequestOperationResult(
                            PlacementRequestOperationStatus.Conflict,
                            "Không thể hoàn tất, vui lòng thử lại.");
                    }
                }

            case AdminPlacementDecision.DismissReport:
                if (dto.Note is not null && dto.Note.Trim().Length > 1000)
                    return new PlacementRequestOperationResult(
                        PlacementRequestOperationStatus.BusinessError,
                        "Ghi chú không được vượt quá 1000 ký tự.");

                using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (_db.Database.IsRelational())
                        {
                            await _db.Database.ExecuteSqlInterpolatedAsync(
                                $"UPDATE [PlacementRequests] SET [Status] = [Status] WHERE [Id] = {requestId}");
                        }

                        var lockedRequest = await _db.PlacementRequests
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(item => item.Id == requestId);

                        if (lockedRequest is not null)
                            await _db.Entry(lockedRequest).ReloadAsync();

                        if (lockedRequest is null
                            || lockedRequest.Status != RequestStatus.InterviewInvited
                            || !lockedRequest.StudentReportedInterviewAt.HasValue
                            || lockedRequest.StudentReportDismissedAt.HasValue)
                        {
                            await transaction.RollbackAsync();
                            return new PlacementRequestOperationResult(
                                PlacementRequestOperationStatus.BusinessError,
                                "Báo cáo phỏng vấn này đã được xử lý.");
                        }

                        var dismissedAt = DateTime.UtcNow;
                        lockedRequest.StudentReportDismissedAt = dismissedAt;
                        lockedRequest.StudentReportDismissSource = confirmationSource;
                        lockedRequest.StudentReportDismissNote = string.IsNullOrWhiteSpace(dto.Note)
                            ? "Báo cáo phỏng vấn đã bị Admin bác bỏ."
                            : CleanString(dto.Note, preserveLineBreaks: true);

                        _logger.LogInformation(
                            "Admin dismissed student interview report: RequestId {RequestId}, AdminUserId {AdminUserId}",
                            requestId,
                            adminUserId);

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return new PlacementRequestOperationResult(PlacementRequestOperationStatus.Success);
                    }
                    catch (Exception exception) when (exception is DbUpdateException or DbException)
                    {
                        await transaction.RollbackAsync();
                        return new PlacementRequestOperationResult(
                            PlacementRequestOperationStatus.Conflict,
                            "Không thể hoàn tất, vui lòng thử lại.");
                    }
                }

            default:
                return new PlacementRequestOperationResult(
                    PlacementRequestOperationStatus.BusinessError,
                    "Quyết định không hợp lệ.");
        }
    }

    public Task<PlacementRequestOperationResult> CancelAcceptedResultAsync(int requestId, int adminUserId, string? note = null)
        => _placementAcceptanceService.CancelAcceptedAsync(requestId, adminUserId, note);

}
