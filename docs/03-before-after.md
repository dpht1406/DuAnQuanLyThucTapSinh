# Báo Cáo So Sánh Trước và Sau Refactor (Before vs After)

## 1. Bảng So Sánh Số Liệu Thực Tế

Mọi số liệu dưới đây được đo đạc trực tiếp từ mã nguồn thực tế và đã lưu tại `docs/evidence/metrics-before.txt` và `docs/evidence/metrics-after.txt`.

| Chỉ Số Đo Lường | Trước Refactor (Baseline) | Sau Refactor (Current) | Biến Đổi / Cải Thiện |
|---|:---:|:---:|---|
| **Tổng số dòng code (LOC)** | 479 dòng | 578 dòng | +99 dòng (+20.7%, do khai báo hằng số tường minh, tách 17 helper methods và phân vùng `#region` rõ ràng) |
| **Số lượng phương thức (Method count)** | 4 methods | 21 methods | Tăng từ 4 hàm nguyên khối lên 21 hàm chuyên biệt theo đúng Single Responsibility |
| **Phương thức dài nhất** | `AcceptAsync` (228 dòng) | `AcceptAsync` (77 dòng) | **Giảm 151 dòng (-66.2%)** |
| **Phương thức dài thứ hai** | `CancelAcceptedAsync` (186 dòng) | `CancelAcceptedAsync` (75 dòng) | **Giảm 111 dòng (-59.7%)** |
| **Độ lồng sâu tối đa (Max Nesting)** | 5 tầng (try/if/if lồng nhau) | 3 tầng trong luồng chính (5 tầng chỉ cục bộ ở transaction wrapper chuẩn) | Đạt độ phẳng cao, các luồng nghiệp vụ đọc theo tuyến tính từ trên xuống |
| **Số đoạn code lặp rõ ràng** | 5 đoạn lặp | 0 đoạn lặp | **Triệt tiêu 100% code lặp** giữa 2 quy trình tiếp nhận và hủy tiếp nhận |
| **Magic strings & magic numbers** | 7 chuỗi cứng rải rác, số 1000 | 0 (tất cả quy tụ thành const) | Toàn bộ thông báo lỗi và giới hạn được quản lý tập trung |
| **Kết quả kiểm thử** | 20/20 test Passed | 20/20 test Passed | **100% xanh, thời gian chạy ~1.2s** |

---

## 2. So Sánh Code Thực Tế của Các Hàm Tiêu Biểu

### Tiêu biểu 1: Điều phối Transaction và xử lý ngoại lệ cơ sở dữ liệu

#### Bản cũ (`git show baseline:InternshipManagement/backend/src/StudentInternshipMgmt.Infrastructure/Services/PlacementAcceptanceService.cs` dòng 248–278 và lặp lại dòng 435–465):
```csharp
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
```

#### Bản mới (trích từ `PlacementAcceptanceService.cs` hiện tại dòng 224–268):
```csharp
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
```
**Giải thích (Tiếng Việt)**:
- Ở bản cũ, toàn bộ khối `try/catch/finally` dài gần 30 dòng với logic bắt ngoại lệ DB, ghi log, try-catch lồng để rollback transaction bị lặp lại từng chữ giữa `AcceptAsync` và `CancelAcceptedAsync`.
- Ở bản mới, logic điều phối giao dịch được đóng gói thành hàm generic `ExecuteInTransactionAsync`. Các hàm nghiệp vụ chỉ cần tập trung vào logic domain mà không phải viết lặp lại mã hạ tầng transaction.

---

### Tiêu biểu 2: Phương thức nghiệp vụ cốt lõi `AcceptAsync`

#### Bản cũ (trích đoạn `AcceptAsync` 228 dòng nguyên khối):
```csharp
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
                    $"UPDATE [PlacementRequests] SET [Status] = [Status] WHERE [Id] = {requestId}");

                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [Students] SET [Status] = [Status] WHERE [Id] = {requestIds.StudentId}");

                if (requestIds.JobPositionId.HasValue)
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE [JobPositions] SET [IsOpen] = [IsOpen] WHERE [Id] = {requestIds.JobPositionId.Value}");
                }
            }

            // ... tiếp tục hơn 170 dòng code xử lý kiểm tra sinh viên, chỉ tiêu, email, lịch sử...
```

#### Bản mới (trích từ `PlacementAcceptanceService.cs` hiện tại dòng 68–144):
```csharp
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
```
**Giải thích (Tiếng Việt)**:
- Phương thức `AcceptAsync` từ một hàm khổng lồ 228 dòng nay trở thành một hàm điều phối cấp cao (high-level orchestrator) rõ ràng dài 77 dòng.
- Mỗi bước nghiệp vụ được diễn đạt bằng một câu lệnh tự giải thích (self-documenting):
  1. `GetRequestSummaryAsync` & `LockRelationalRowsForAcceptanceAsync`: Truy vấn nhanh và khóa bản ghi chống xung đột.
  2. `ValidateAcceptanceActorPermission`: Kiểm tra phân quyền Doanh nghiệp vs Admin.
  3. `ValidateJobPositionForAcceptanceAsync`: Kiểm tra trạng thái duyệt và chỉ tiêu còn lại.
  4. `ApplyRequestAcceptance` & `ApplyStudentAcceptance`: Cập nhật trạng thái thực thể.
  5. `AddAcceptanceStatusHistory`: Ghi vết lịch sử trạng thái sinh viên.
  6. `AutoRejectOtherStudentRequestsAsync`: Tự động từ chối các đơn mở khác của sinh viên.
  7. `CreateAcceptanceNotification`: Gửi thông báo hệ thống.
  8. `_emailNotifier.NotifyResultAsync`: Bắn email kết quả sau khi commit thành công.

---

### Tiêu biểu 3: Khôi phục vị trí tuyển dụng và chuẩn hóa điều kiện trạng thái

#### Bản cũ (trích đoạn `CancelAcceptedAsync` dòng 402–422):
```csharp
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
```

#### Bản mới (trích từ `PlacementAcceptanceService.cs` hiện tại dòng 540–559):
```csharp
    private async Task ReopenJobPositionIfCapacityAvailableAsync(int? jobPositionId, int currentStudentId)
    {
        if (!jobPositionId.HasValue)
            return;

        var position = await _db.JobPositions.FirstOrDefaultAsync(item => item.Id == jobPositionId.Value);
        if (position is null || position.IsOpen || position.ApprovalStatus != JobPositionApprovalStatus.Approved)
            return;

        var remainingAcceptedCount = await _db.Students.CountAsync(item =>
            item.JobPositionId == position.Id
            && item.Id != currentStudentId
            && !item.IsDeleted
            && AcceptedStatuses.Contains(item.Status));

        if (remainingAcceptedCount < position.Quantity)
        {
            position.IsOpen = true;
        }
    }
```
**Giải thích (Tiếng Việt)**:
- Ở bản cũ, đoạn kiểm tra này nằm lồng sâu 4 tầng bên trong `CancelAcceptedAsync`. Đồng thời, thay vì sử dụng mảng trạng thái chuẩn `AcceptedStatuses` đã khai báo ở đầu class, đoạn code cũ lại viết thủ công 3 phép so sánh `||` (`item.Status == StudentStatus.Accepted || ...`), gây phân mảnh logic kiểm tra trạng thái sinh viên.
- Ở bản mới, hàm được tách riêng với guard clauses rõ ràng (`if (!jobPositionId.HasValue) return;`, `if (position is null || position.IsOpen...) return;`) và tái sử dụng mảng `AcceptedStatuses.Contains(item.Status)`.
