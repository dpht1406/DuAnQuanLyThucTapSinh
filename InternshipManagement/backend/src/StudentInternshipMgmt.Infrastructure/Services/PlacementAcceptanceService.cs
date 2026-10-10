using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
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

    private static readonly StudentStatus[] AcceptedStatuses =
    {
        StudentStatus.Accepted,
        StudentStatus.Interning,
        StudentStatus.Completed
    };

    private readonly AppDbContext _db;
    private readonly ILogger<PlacementAcceptanceService> _logger;

    public PlacementAcceptanceService(AppDbContext db, ILogger<PlacementAcceptanceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PlacementRequestOperationResult> AcceptAsync(
        int requestId,
        int? scopeCompanyId,
        ActorType actor,
        int actorUserId,
        string? note,
        string? confirmationSource)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _db.Database.BeginTransactionAsync();

            var requestIds = await _db.PlacementRequests
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(request => request.Id == requestId)
                .Select(request => new { request.StudentId, request.JobPositionId, request.CompanyId })
                .FirstOrDefaultAsync();

            if (requestIds is null
                || scopeCompanyId.HasValue && requestIds.CompanyId != scopeCompanyId.Value)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.NotFound,
                    "Không tìm thấy đơn ứng tuyển.");
            }

            if (_db.Database.IsRelational())
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {requestIds.StudentId}");

                if (requestIds.JobPositionId.HasValue)
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE [JobPositions] SET [IsOpen] = [IsOpen] WHERE [Id] = {requestIds.JobPositionId.Value}");
                }
            }

            var request = await _db.PlacementRequests
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == requestId);

            if (request is null
                || scopeCompanyId.HasValue && request.CompanyId != scopeCompanyId.Value)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.NotFound,
                    "Không tìm thấy đơn ứng tuyển.");
            }

            if (request.Status is not (RequestStatus.Pending or RequestStatus.InterviewInvited))
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    "Đơn này đã được xử lý.");
            }

            var student = await _db.Students
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == request.StudentId);

            if (student is null || student.IsDeleted || student.Status != StudentStatus.NoCompany)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.Conflict,
                    StudentConflict);
            }

            JobPosition? position = null;
            var acceptedCount = 0;
            if (request.JobPositionId.HasValue)
            {
                position = await _db.JobPositions
                    .FirstOrDefaultAsync(item => item.Id == request.JobPositionId.Value);

                if (position is null || position.ApprovalStatus != JobPositionApprovalStatus.Approved)
                {
                    return await RollbackWithResultAsync(
                        transaction,
                        PlacementRequestOperationStatus.BusinessError,
                        "Vị trí tuyển dụng chưa được duyệt.");
                }

                acceptedCount = await _db.Students.CountAsync(item =>
                    item.JobPositionId == position.Id && AcceptedStatuses.Contains(item.Status));
                if (acceptedCount >= position.Quantity)
                {
                    return await RollbackWithResultAsync(
                        transaction,
                        PlacementRequestOperationStatus.Conflict,
                        CapacityConflict);
                }
            }

            var company = await _db.Companies.FirstOrDefaultAsync(item => item.Id == request.CompanyId);
            if (company is null)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    "Không tìm thấy doanh nghiệp.");
            }

            var now = DateTime.UtcNow;
            request.Status = RequestStatus.Accepted;
            request.ResultNote = CleanString(note);
            request.ResultActorType = actor;
            request.ReviewedBy = actorUserId;
            request.ReviewedAt = now;
            request.ConfirmationSource = confirmationSource;

            student.Status = StudentStatus.Accepted;
            student.CompanyId = request.CompanyId;
            student.JobPositionId = request.JobPositionId;
            student.UpdatedAt = now;

            var historyNote = actor == ActorType.Company
                ? position is null
                    ? "Doanh nghiệp xác nhận Đạt"
                    : $"Doanh nghiệp xác nhận Đạt cho vị trí {position.Title}"
                : position is null
                    ? "Admin xác nhận Đạt thay doanh nghiệp"
                    : $"Admin xác nhận Đạt thay doanh nghiệp cho vị trí {position.Title}";
            _db.StatusHistories.Add(new StatusHistory
            {
                StudentId = student.Id,
                FromStatus = StudentStatus.NoCompany,
                ToStatus = StudentStatus.Accepted,
                CompanyId = request.CompanyId,
                Note = historyNote,
                ChangedAt = now,
                ChangedBy = actorUserId,
                ChangedByType = actor
            });

            var otherOpenRequests = await _db.PlacementRequests
                .IgnoreQueryFilters()
                .Where(item => item.StudentId == student.Id
                    && item.Id != request.Id
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

            if (position is not null && acceptedCount + 1 == position.Quantity)
                position.IsOpen = false;

            var positionDescription = position is null ? string.Empty : $" tại vị trí {position.Title}";
            _db.Notifications.Add(new Notification
            {
                Type = NotificationType.StudentAcceptedByCompany,
                Title = "Doanh nghiệp nhận sinh viên",
                Message = $"{student.FullName} ({student.StudentCode}) đã được {company.Name} nhận{positionDescription}.",
                StudentId = student.Id,
                CompanyId = company.Id,
                CompanyName = company.Name,
                CreatedAt = now
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Placement request accepted: RequestId {RequestId}, StudentId {StudentId}, CompanyId {CompanyId}, Actor {Actor}",
                request.Id, student.Id, company.Id, actor);
            return new PlacementRequestOperationResult(PlacementRequestOperationStatus.Success);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            _logger.LogError(
                exception,
                "Placement acceptance transaction failed: RequestId {RequestId}",
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
                        "Placement acceptance rollback failed: RequestId {RequestId}",
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

    public async Task<PlacementRequestOperationResult> CancelAcceptedAsync(int requestId, int adminUserId, string? note)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _db.Database.BeginTransactionAsync();

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
                        "Không tìm thấy đơn ứng tuyển.");
                }

                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {requestLock.StudentId}");

                if (requestLock.JobPositionId.HasValue)
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE [JobPositions] SET [IsOpen] = [IsOpen] WHERE [Id] = {requestLock.JobPositionId.Value}");
                }
            }

            var request = await _db.PlacementRequests
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == requestId);

            if (request is null)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.NotFound,
                    "Không tìm thấy đơn ứng tuyển.");
            }

            if (request.Status != RequestStatus.Accepted)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    "Chỉ có thể hủy kết quả tiếp nhận khi đơn đang ở trạng thái Đạt.");
            }

            var student = await _db.Students
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id == request.StudentId);

            if (student is null)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.NotFound,
                    "Không tìm thấy sinh viên.");
            }

            if (student.IsDeleted)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    "Sinh viên đã bị xóa, không thể hủy kết quả.");
            }

            if (student.Status != StudentStatus.Accepted
                || student.CompanyId != request.CompanyId
                || student.JobPositionId != request.JobPositionId)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    "Dữ liệu sinh viên không khớp với đơn, không thể hủy tự động.");
            }

            if (note is not null && note.Trim().Length > 1000)
            {
                return await RollbackWithResultAsync(
                    transaction,
                    PlacementRequestOperationStatus.BusinessError,
                    "Ghi chú không được vượt quá 1000 ký tự.");
            }

            var cancellationNote = string.IsNullOrWhiteSpace(note)
                ? "Admin hủy kết quả tiếp nhận sinh viên."
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
            request.ConfirmationSource = "admin-cancel-accepted";

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
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            _logger.LogError(
                exception,
                "Placement acceptance cancellation failed: RequestId {RequestId}",
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
                        "Placement acceptance cancellation rollback failed: RequestId {RequestId}",
                        requestId);
                }
            }

            return new PlacementRequestOperationResult(
                PlacementRequestOperationStatus.Conflict,
                "Không thể hoàn tất, vui lòng thử lại.");
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private static async Task<PlacementRequestOperationResult> RollbackWithResultAsync(
        IDbContextTransaction transaction,
        PlacementRequestOperationStatus status,
        string error)
    {
        await transaction.RollbackAsync();
        return new PlacementRequestOperationResult(status, error);
    }

    private static string? CleanString(string? value, bool preserveLineBreaks = false) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
