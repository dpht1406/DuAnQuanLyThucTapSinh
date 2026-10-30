using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public sealed class PlacementAcceptanceService : IPlacementAcceptanceService
{
    private const string CapacityConflict = "Vị trí đã đủ chỉ tiêu.";
    private const string StudentConflict = "Sinh viên đã được doanh nghiệp khác nhận hoặc đang được Admin xử lý.";
    private const string RetryConflict = "Không thể hoàn tất, vui lòng thử lại.";
    private const string OtherRequestsResult = "Sinh viên đã được doanh nghiệp khác nhận";

    private const string RequestNotFoundMessage = "Không tìm thấy đơn ứng tuyển.";
    private const string InterviewReportAlreadyHandledMessage = "Báo cáo phỏng vấn này đã được xử lý.";
    private const string RequestAlreadyHandledMessage = "Đơn này đã được xử lý.";
    private const string JobPositionNotApprovedMessage = "Vị trí tuyển dụng chưa được duyệt.";
    private const string CompanyNotFoundMessage = "Không tìm thấy doanh nghiệp.";
    private const string OnlyAcceptedCanBeCanceledMessage = "Chỉ có thể hủy kết quả tiếp nhận khi đơn đang ở trạng thái Đạt.";
    private const string StudentNotFoundMessage = "Không tìm thấy sinh viên.";
    private const string StudentDeletedMessage = "Sinh viên đã bị xóa, không thể hủy kết quả.";
    private const string StudentDataMismatchMessage = "Dữ liệu sinh viên không khớp với đơn, không thể hủy tự động.";
    private const string NoteTooLongMessage = "Ghi chú không được vượt quá 1000 ký tự.";
    private const string DefaultCancellationNote = "Admin hủy kết quả tiếp nhận sinh viên.";

    private const string AdminCancelConfirmationSource = "admin-cancel-accepted";
    private const string StudentAcceptedNotificationTitle = "Doanh nghiệp nhận sinh viên";
    private const int MaxCancellationNoteLength = 1000;

    private static readonly StudentStatus[] AcceptedStatuses =
    {
        StudentStatus.Accepted,
        StudentStatus.Interning,
        StudentStatus.Completed
    };

    private readonly AppDbContext _db;
    private readonly ILogger<PlacementAcceptanceService> _logger;
    private readonly PlacementRequestEmailNotifier _emailNotifier;

    public PlacementAcceptanceService(AppDbContext db, ILogger<PlacementAcceptanceService> logger)
        : this(
            db,
            logger,
            new PlacementRequestEmailNotifier(
                new NoOpEmailSender(NullLogger<NoOpEmailSender>.Instance),
                NullLogger<PlacementRequestEmailNotifier>.Instance))
    {
    }

    public PlacementAcceptanceService(
        AppDbContext db,
        ILogger<PlacementAcceptanceService> logger,
        PlacementRequestEmailNotifier emailNotifier)
    {
        _db = db;
        _logger = logger;
        _emailNotifier = emailNotifier;
    }

    public async Task<PlacementRequestOperationResult> AcceptAsync(
        int requestId,
        int? scopeCompanyId,
        ActorType actor,
        int actorUserId,
        string? note,
        string? confirmationSource)
    {
        PlacementRequest? acceptedRequest = null;

        var result = await ExecuteInTransactionAsync(requestId, "Placement acceptance", async transaction =>
        {
            var requestSummary = await GetRequestSummaryAsync(requestId);
            if (requestSummary is null || (scopeCompanyId.HasValue && requestSummary.CompanyId != scopeCompanyId.Value))
            {
                return await RollbackWithResultAsync(transaction, PlacementRequestOperationStatus.NotFound, RequestNotFoundMessage);
            }

            await LockRelationalRowsForAcceptanceAsync(requestId, requestSummary.StudentId, requestSummary.JobPositionId);

            var request = await GetPlacementRequestByIdAsync(requestId);
            if (request is null || (scopeCompanyId.HasValue && request.CompanyId != scopeCompanyId.Value))
            {
                return await RollbackWithResultAsync(transaction, PlacementRequestOperationStatus.NotFound, RequestNotFoundMessage);
            }

            var actorValidationError = ValidateAcceptanceActorPermission(actor, request);
            if (actorValidationError is not null)
            {
                return await RollbackWithResultAsync(transaction, PlacementRequestOperationStatus.BusinessError, actorValidationError);
            }

            var student = await GetStudentByIdAsync(request.StudentId);
            if (student is null || student.IsDeleted || student.Status != StudentStatus.NoCompany)
            {
                return await RollbackWithResultAsync(transaction, PlacementRequestOperationStatus.Conflict, StudentConflict);
            }

            var (positionValidationError, position, acceptedCount) = await ValidateJobPositionForAcceptanceAsync(request.JobPositionId);
            if (positionValidationError is not null)
            {
                return await RollbackWithResultAsync(transaction, positionValidationError.Value.Status, positionValidationError.Value.Message);
            }

            var company = await _db.Companies.FirstOrDefaultAsync(item => item.Id == request.CompanyId);
            if (company is null)
            {
                return await RollbackWithResultAsync(transaction, PlacementRequestOperationStatus.BusinessError, CompanyNotFoundMessage);
            }
            request.Company = company;

            var now = DateTime.UtcNow;
            ApplyRequestAcceptance(request, note, actor, actorUserId, confirmationSource, now);
            ApplyStudentAcceptance(student, request.CompanyId, request.JobPositionId, now);
            AddAcceptanceStatusHistory(student.Id, request.CompanyId, actor, actorUserId, position?.Title, now);
            await AutoRejectOtherStudentRequestsAsync(student.Id, request.Id, now);
            CheckAndCloseJobPositionIfFull(position, acceptedCount);
            CreateAcceptanceNotification(student, company, position?.Title, now);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            acceptedRequest = request;
            _logger.LogInformation(
                "Placement request accepted: RequestId {RequestId}, StudentId {StudentId}, CompanyId {CompanyId}, Actor {Actor}",
                request.Id, student.Id, company.Id, actor);

            return new PlacementRequestOperationResult(PlacementRequestOperationStatus.Success);
        });

        if (result.Status == PlacementRequestOperationStatus.Success && acceptedRequest is not null)
        {
            await _emailNotifier.NotifyResultAsync(acceptedRequest, passed: true, acceptedRequest.ResultNote);
        }

        return result;
    }

    public async Task<PlacementRequestOperationResult> CancelAcceptedAsync(int requestId, int adminUserId, string? note)
    {
        return await ExecuteInTransactionAsync(requestId, "Placement acceptance cancellation", async transaction =>
        {
            if (_db.Database.IsRelational())
            {
                var requestLock = await _db.PlacementRequests
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.Id == requestId)
                    .Select(item => new { item.StudentId, item.JobPositionId, item.CompanyId, item.Status })
                    .FirstOrDefaultAsync();

                if (requestLock is null)
                {
                    return await RollbackWithResultAsync(
                        transaction,
                        PlacementRequestOperationStatus.NotFound,
                        RequestNotFoundMessage);
                }

                await LockRelationalRowsForCancellationAsync(requestLock.StudentId, requestLock.JobPositionId);
            }

            var request = await _db.PlacementRequests
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == requestId);

            if (request is null)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.NotFound,
                    RequestNotFoundMessage);
            }

            if (request.Status != RequestStatus.Accepted)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    OnlyAcceptedCanBeCanceledMessage);
            }

            var student = await _db.Students
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == request.StudentId);

            if (student is null)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.NotFound,
                    StudentNotFoundMessage);
            }

            if (student.IsDeleted)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    StudentDeletedMessage);
            }

            if (student.Status != StudentStatus.Accepted
                || student.CompanyId != request.CompanyId
                || student.JobPositionId != request.JobPositionId)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    StudentDataMismatchMessage);
            }

            if (note is not null && note.Trim().Length > MaxCancellationNoteLength)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    NoteTooLongMessage);
            }

            var cancellationNote = string.IsNullOrWhiteSpace(note)
                ? DefaultCancellationNote
                : CleanString(note, preserveLineBreaks: true)!;

            var now = DateTime.UtcNow;
            student.Status = StudentStatus.NoCompany;
            student.CompanyId = null;
            student.JobPositionId = null;
            student.UpdatedAt = now;

            request.Status = RequestStatus.Rejected;
            request.ResultNote = cancellationNote;
            request.ResultActorType = ActorType.Admin;
            request.ReviewedBy = adminUserId;
            request.ReviewedAt = now;
            request.ConfirmationSource = AdminCancelConfirmationSource;

            _db.StatusHistories.Add(new StatusHistory
            {
                StudentId = student.Id,
                FromStatus = StudentStatus.Accepted,
                ToStatus = StudentStatus.NoCompany,
                CompanyId = request.CompanyId,
                Note = cancellationNote,
                ChangedAt = now,
                ChangedBy = adminUserId,
                ChangedByType = ActorType.Admin
            });

            if (request.JobPositionId.HasValue)
            {
                var position = await _db.JobPositions.FirstOrDefaultAsync(item => item.Id == request.JobPositionId.Value);
                if (position is not null
                    && !position.IsOpen
                    && position.ApprovalStatus == JobPositionApprovalStatus.Approved)
                {
                    var remainingAcceptedCount = await _db.Students.CountAsync(item =>
                        item.JobPositionId == position.Id
                        && item.Id != student.Id
                        && !item.IsDeleted
                        && (item.Status == StudentStatus.Accepted
                            || item.Status == StudentStatus.Interning
                            || item.Status == StudentStatus.Completed));

                    if (remainingAcceptedCount < position.Quantity)
                    {
                        position.IsOpen = true;
                    }
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Placement request accepted result canceled: RequestId {RequestId}, StudentId {StudentId}, AdminUserId {AdminUserId}",
                requestId,
                student.Id,
                adminUserId);

            return new PlacementRequestOperationResult(PlacementRequestOperationStatus.Success);
        });
    }

    #region Helper Methods - Queries & Locking

    private async Task<PlacementRequestOperationResult> ExecuteInTransactionAsync(
        int requestId,
        string operationName,
        Func<IDbContextTransaction, Task<PlacementRequestOperationResult>> action)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _db.Database.BeginTransactionAsync();
            return await action(transaction);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            _logger.LogError(
                exception,
                "{OperationName} transaction failed: RequestId {RequestId}",
                operationName,
                requestId);

            if (transaction is not null)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackException) when (rollbackException is DbUpdateException or DbException)
                {
                    _logger.LogError(
                        rollbackException,
                        "{OperationName} rollback failed: RequestId {RequestId}",
                        operationName,
                        requestId);
                }
            }

            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.Conflict,
                RetryConflict);
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task LockRelationalRowsForAcceptanceAsync(int requestId, int studentId, int? jobPositionId)
    {
        if (!_db.Database.IsRelational())
            return;

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [PlacementRequests] SET [Status] = [Status] WHERE [Id] = {requestId}");

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}");

        if (jobPositionId.HasValue)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [JobPositions] SET [IsOpen] = [IsOpen] WHERE [Id] = {jobPositionId.Value}");
        }
    }

    private async Task LockRelationalRowsForCancellationAsync(int studentId, int? jobPositionId)
    {
        if (!_db.Database.IsRelational())
            return;

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {studentId}");

        if (jobPositionId.HasValue)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [JobPositions] SET [IsOpen] = [IsOpen] WHERE [Id] = {jobPositionId.Value}");
        }
    }

    private Task<PlacementRequest?> GetPlacementRequestByIdAsync(int requestId) =>
        _db.PlacementRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == requestId);

    private Task<Student?> GetStudentByIdAsync(int studentId) =>
        _db.Students
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == studentId);

    private async Task<RequestSummary?> GetRequestSummaryAsync(int requestId)
    {
        var raw = await _db.PlacementRequests
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(request => request.Id == requestId)
            .Select(request => new { request.StudentId, request.JobPositionId, request.CompanyId })
            .FirstOrDefaultAsync();

        return raw is null ? null : new RequestSummary(raw.StudentId, raw.JobPositionId, raw.CompanyId);
    }

    private sealed record RequestSummary(int StudentId, int? JobPositionId, int CompanyId);

    #endregion

    #region Helper Methods - Acceptance Business Logic

    private static string? ValidateAcceptanceActorPermission(ActorType actor, PlacementRequest request)
    {
        if (actor == ActorType.Admin
            && (request.Status != RequestStatus.InterviewInvited
                || request.StudentReportedInterviewAt is null
                || request.StudentReportDismissedAt is not null))
        {
            return InterviewReportAlreadyHandledMessage;
        }

        if (request.Status is not (RequestStatus.Pending or RequestStatus.InterviewInvited))
        {
            return RequestAlreadyHandledMessage;
        }

        return null;
    }

    private async Task<((PlacementRequestOperationStatus Status, string Message)? Error, JobPosition? Position, int AcceptedCount)>
        ValidateJobPositionForAcceptanceAsync(int? jobPositionId)
    {
        if (!jobPositionId.HasValue)
            return (null, null, 0);

        var position = await _db.JobPositions
            .FirstOrDefaultAsync(item => item.Id == jobPositionId.Value);

        if (position is null || position.ApprovalStatus != JobPositionApprovalStatus.Approved)
        {
            return ((PlacementRequestOperationStatus.BusinessError, JobPositionNotApprovedMessage), null, 0);
        }

        var acceptedCount = await _db.Students.CountAsync(item =>
            item.JobPositionId == position.Id && AcceptedStatuses.Contains(item.Status));

        if (acceptedCount >= position.Quantity)
        {
            return ((PlacementRequestOperationStatus.Conflict, CapacityConflict), null, acceptedCount);
        }

        return (null, position, acceptedCount);
    }

    private static void ApplyRequestAcceptance(
        PlacementRequest request,
        string? note,
        ActorType actor,
        int actorUserId,
        string? confirmationSource,
        DateTime now)
    {
        request.Status = RequestStatus.Accepted;
        request.ResultNote = CleanString(note);
        request.ResultActorType = actor;
        request.ReviewedBy = actorUserId;
        request.ReviewedAt = now;
        request.ConfirmationSource = confirmationSource;
    }

    private static void ApplyStudentAcceptance(Student student, int companyId, int? jobPositionId, DateTime now)
    {
        student.Status = StudentStatus.Accepted;
        student.CompanyId = companyId;
        student.JobPositionId = jobPositionId;
        student.UpdatedAt = now;
    }

    private void AddAcceptanceStatusHistory(
        int studentId,
        int companyId,
        ActorType actor,
        int actorUserId,
        string? positionTitle,
        DateTime now)
    {
        var historyNote = BuildAcceptanceHistoryNote(actor, positionTitle);
        _db.StatusHistories.Add(new StatusHistory
        {
            StudentId = studentId,
            FromStatus = StudentStatus.NoCompany,
            ToStatus = StudentStatus.Accepted,
            CompanyId = companyId,
            Note = historyNote,
            ChangedAt = now,
            ChangedBy = actorUserId,
            ChangedByType = actor
        });
    }

    private static string BuildAcceptanceHistoryNote(ActorType actor, string? positionTitle)
    {
        var targetPosition = string.IsNullOrEmpty(positionTitle) ? string.Empty : $" cho vị trí {positionTitle}";
        return actor == ActorType.Company
            ? $"Doanh nghiệp xác nhận Đạt{targetPosition}"
            : $"Admin xác nhận Đạt thay doanh nghiệp{targetPosition}";
    }

    private async Task AutoRejectOtherStudentRequestsAsync(int studentId, int currentRequestId, DateTime now)
    {
        var otherOpenRequests = await _db.PlacementRequests
            .IgnoreQueryFilters()
            .Where(item => item.StudentId == studentId
                && item.Id != currentRequestId
                && (item.Status == RequestStatus.Pending || item.Status == RequestStatus.InterviewInvited))
            .ToListAsync();

        foreach (var otherRequest in otherOpenRequests)
        {
            otherRequest.Status = RequestStatus.Rejected;
            otherRequest.ResultActorType = ActorType.System;
            otherRequest.ResultNote = OtherRequestsResult;
            otherRequest.ReviewedAt = now;
            otherRequest.ReviewedBy = null;
        }
    }

    private static void CheckAndCloseJobPositionIfFull(JobPosition? position, int acceptedCount)
    {
        if (position is not null && acceptedCount + 1 == position.Quantity)
        {
            position.IsOpen = false;
        }
    }

    private void CreateAcceptanceNotification(Student student, Company company, string? positionTitle, DateTime now)
    {
        var positionDescription = string.IsNullOrEmpty(positionTitle) ? string.Empty : $" tại vị trí {positionTitle}";
        _db.Notifications.Add(new Notification
        {
            Type = NotificationType.StudentAcceptedByCompany,
            Title = StudentAcceptedNotificationTitle,
            Message = $"{student.FullName} ({student.StudentCode}) đã được {company.Name} nhận{positionDescription}.",
            StudentId = student.Id,
            CompanyId = company.Id,
            CompanyName = company.Name,
            CreatedAt = now
        });
    }

    #endregion

    private static async Task<PlacementRequestOperationResult> RollbackWithResultAsync(
        IDbContextTransaction transaction,
        PlacementRequestOperationStatus status,
        string error)
    {
        await transaction.RollbackAsync();
        return new PlacementRequestOperationResult(status, error);
    }

    private static string? CleanString(string? value, bool preserveLineBreaks = false)
    {
        // Parameter preserveLineBreaks is documented as known issue BUG-PRE-01 (see docs/05-bug-log.md)
        _ = preserveLineBreaks;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
