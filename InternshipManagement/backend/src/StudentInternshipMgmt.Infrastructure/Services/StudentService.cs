using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Students;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly AppDbContext _db;
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(
        @"^0\d{9}$", RegexOptions.Compiled);

    public StudentService(AppDbContext db)
    {
        _db = db;
    }

    // ---------- 2. CRUD & tìm kiếm/lọc/xoá mềm ----------

    public async Task<PagedResult<StudentDto>> GetStudentsAsync(StudentFilterDto filter)
    {
        // Global query filter (IsDeleted = false) đã cấu hình ở Giai đoạn 1,
        // nên không cần .Where(x => !x.IsDeleted) ở đây nữa.
        var query = _db.Students
            .Include(s => s.Company)
            .Include(s => s.JobPosition)
            .Include(s => s.User)
            .AsQueryable();

        query = ApplyStudentFilter(query, filter);

        var totalCount = await query.CountAsync();

        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var entities = await query
            .OrderBy(s => s.StudentCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entities.Select(MapToDto).ToList();

        return new PagedResult<StudentDto>(items, totalCount, pageNumber, pageSize);
    }

    private static IQueryable<Student> ApplyStudentFilter(IQueryable<Student> query, StudentFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(s =>
                s.StudentCode.ToLower().Contains(search) ||
                s.FullName.ToLower().Contains(search));
        }

        if (filter.Status.HasValue)
            query = query.Where(s => s.Status == filter.Status.Value);

        if (filter.CompanyId.HasValue)
            query = query.Where(s => s.CompanyId == filter.CompanyId.Value);

        if (filter.HasAccount.HasValue)
            query = query.Where(s => (s.User != null) == filter.HasAccount.Value);

        return query;
    }

    public async Task<StudentDto?> GetStudentByIdAsync(int id)
    {
        var student = await _db.Students
            .Include(s => s.Company)
            .Include(s => s.JobPosition)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        return student is null ? null : MapToDto(student);
    }

    public async Task<(bool Success, string? Error, StudentDto? Data)> CreateStudentAsync(CreateStudentDto dto)
    {
        var codeExists = await _db.Students.AnyAsync(s => s.StudentCode == dto.StudentCode);
        if (codeExists)
            return (false, $"StudentCode '{dto.StudentCode}' đã tồn tại.", null);

        var student = new Student
        {
            StudentCode = dto.StudentCode,
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Major = dto.Major,
            ClassName = dto.ClassName,
            Status = StudentStatus.NoCompany,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        return (true, null, MapToDto(student));
    }

    public async Task<(bool Success, string? Error)> UpdateStudentAsync(int id, UpdateStudentDto dto)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == id);
        if (student is null)
            return (false, "Không tìm thấy sinh viên.");

        // Chỉ sửa thông tin cơ bản — không đụng StudentCode, Status, CompanyId, JobPositionId.
        student.FullName = dto.FullName;
        student.Email = dto.Email;
        student.PhoneNumber = dto.PhoneNumber;
        student.Major = dto.Major;
        student.ClassName = dto.ClassName;
        student.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteStudentAsync(int id)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student is null)
            return (false, "Không tìm thấy sinh viên.");

        student.IsDeleted = true;

        if (student.User is not null)
            student.User.IsActive = false;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ---------- 3. Tạo tài khoản hàng loạt ----------

    public async Task<CreateAccountsResultDto> CreateAccountsAsync(List<int> studentIds)
    {
        var result = new CreateAccountsResultDto();

        // Student không có cột UserId — quan hệ 1-1 nằm ở User.StudentId (FK),
        // Student.User chỉ là navigation ngược, nên phải Include(User) để biết
        // sinh viên đã có tài khoản hay chưa.
        var students = await _db.Students
            .Include(s => s.User)
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync();

        foreach (var student in students)
        {
            if (student.User is not null)
            {
                result.SkippedCount++;
                continue;
            }

            var plainPassword = GenerateRandomPassword();
            var user = new User
            {
                Username = student.StudentCode,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                Role = UserRole.User,
                MustChangePassword = true,
                IsActive = true,
                StudentId = student.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);

            result.SuccessCount++;
            result.CreatedAccounts.Add(new CreatedAccountDto
            {
                StudentCode = student.StudentCode,
                PlainPassword = plainPassword // chỉ tồn tại trong response, KHÔNG lưu DB
            });
        }

        await _db.SaveChangesAsync(); // 1 lần cho cả loạt — EF tự fixup Student.User qua User.StudentId
        return result;
    }

    public async Task<CreateAccountsResultDto> CreateAccountsByFilterAsync(StudentFilterDto filter)
    {
        var query = _db.Students
            .Include(s => s.User)
            .AsQueryable();

        query = ApplyStudentFilter(query, filter);

        var studentIds = await query.Select(s => s.Id).ToListAsync();

        return await CreateAccountsAsync(studentIds);
    }

    private static string GenerateRandomPassword(int length = 10)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // bỏ I, O dễ nhầm
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = upper + lower + digits;

        Span<byte> buffer = stackalloc byte[length];
        RandomNumberGenerator.Fill(buffer);

        var chars = new char[length];
        // đảm bảo có ít nhất 1 hoa, 1 thường, 1 số
        chars[0] = upper[buffer[0] % upper.Length];
        chars[1] = lower[buffer[1] % lower.Length];
        chars[2] = digits[buffer[2] % digits.Length];
        for (int i = 3; i < length; i++)
            chars[i] = all[buffer[i] % all.Length];

        // trộn ngẫu nhiên vị trí
        for (int i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    // ---------- 4. Import Excel/CSV ----------

    public async Task<ImportResultDto> ImportStudentsAsync(Stream fileStream, string fileName)
    {
        var result = new ImportResultDto();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        List<ImportRow> rows;
        try
        {
            rows = extension switch
            {
                ".xlsx" or ".xls" => ReadExcel(fileStream),
                ".csv" => ReadCsv(fileStream),
                _ => throw new InvalidOperationException("Chỉ hỗ trợ file .xlsx, .xls hoặc .csv.")
            };
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportErrorDto { RowNumber = 0, Reason = $"Không đọc được file: {ex.Message}" });
            return result;
        }

        var existingCodes = new HashSet<string>(
            await _db.Students.Select(s => s.StudentCode).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        var codesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toInsert = new List<Student>();

        foreach (var row in rows)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(row.StudentCode)) errors.Add("Thiếu StudentCode.");
            if (string.IsNullOrWhiteSpace(row.FullName)) errors.Add("Thiếu FullName.");
            if (string.IsNullOrWhiteSpace(row.Email)) errors.Add("Thiếu Email.");
            if (string.IsNullOrWhiteSpace(row.PhoneNumber)) errors.Add("Thiếu PhoneNumber.");
            if (string.IsNullOrWhiteSpace(row.Major)) errors.Add("Thiếu Major.");
            if (string.IsNullOrWhiteSpace(row.ClassName)) errors.Add("Thiếu ClassName.");

            if (!string.IsNullOrWhiteSpace(row.Email) && !EmailRegex.IsMatch(row.Email))
                errors.Add("Email không đúng định dạng.");

            if (!string.IsNullOrWhiteSpace(row.PhoneNumber) && !PhoneRegex.IsMatch(row.PhoneNumber))
                errors.Add("PhoneNumber không đúng định dạng (10 số, bắt đầu bằng 0).");

            if (errors.Count == 0)
            {
                if (existingCodes.Contains(row.StudentCode))
                    errors.Add($"StudentCode '{row.StudentCode}' đã tồn tại trong hệ thống.");
                else if (codesInFile.Contains(row.StudentCode))
                    errors.Add($"StudentCode '{row.StudentCode}' bị trùng ngay trong file import.");
            }

            if (errors.Count > 0)
            {
                result.Errors.Add(new ImportErrorDto
                {
                    RowNumber = row.RowNumber,
                    Reason = string.Join(" ", errors)
                });
                continue; // partial: bỏ qua dòng lỗi, không chặn cả file
            }

            codesInFile.Add(row.StudentCode);
            toInsert.Add(new Student
            {
                StudentCode = row.StudentCode,
                FullName = row.FullName,
                Email = row.Email,
                PhoneNumber = row.PhoneNumber,
                Major = row.Major,
                ClassName = row.ClassName,
                Status = StudentStatus.NoCompany,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (toInsert.Count > 0)
        {
            _db.Students.AddRange(toInsert);
            await _db.SaveChangesAsync();
        }

        result.SuccessCount = toInsert.Count;
        return result;
    }

    private record ImportRow(int RowNumber, string? StudentCode, string? FullName,
        string? Email, string? PhoneNumber, string? Major, string? ClassName);

    private static List<ImportRow> ReadExcel(Stream stream)
    {
        var rows = new List<ImportRow>();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        // Dòng 1 = header: StudentCode, FullName, Email, PhoneNumber, Major, ClassName
        var firstDataRow = 2;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = firstDataRow; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            if (row.IsEmpty()) continue;

            rows.Add(new ImportRow(
                RowNumber: r,
                StudentCode: row.Cell(1).GetString().Trim(),
                FullName: row.Cell(2).GetString().Trim(),
                Email: row.Cell(3).GetString().Trim(),
                PhoneNumber: row.Cell(4).GetString().Trim(),
                Major: row.Cell(5).GetString().Trim(),
                ClassName: row.Cell(6).GetString().Trim()
            ));
        }

        return rows;
    }

    private static List<ImportRow> ReadCsv(Stream stream)
    {
        var rows = new List<ImportRow>();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        int rowNumber = 1; // dòng 1 là header, dữ liệu bắt đầu từ dòng 2
        while (csv.Read())
        {
            rowNumber++;
            rows.Add(new ImportRow(
                RowNumber: rowNumber,
                StudentCode: csv.GetField("StudentCode")?.Trim(),
                FullName: csv.GetField("FullName")?.Trim(),
                Email: csv.GetField("Email")?.Trim(),
                PhoneNumber: csv.GetField("PhoneNumber")?.Trim(),
                Major: csv.GetField("Major")?.Trim(),
                ClassName: csv.GetField("ClassName")?.Trim()
            ));
        }

        return rows;
    }

    // ---------- 5. Đổi trạng thái ----------

    public async Task<(bool Success, string? Error)> ChangeStatusAsync(int studentId, ChangeStatusDto dto, int changedByUserId, bool isAdmin)
    {
        if (!Enum.IsDefined(typeof(StudentStatus), dto.NewStatus))
            return (false, "NewStatus không hợp lệ.");

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        if (student is null)
            return (false, "Không tìm thấy sinh viên.");

        var fromStatus = student.Status;
        var toStatus = dto.NewStatus;

        if (fromStatus == toStatus)
            return (false, "Trạng thái mới phải khác trạng thái hiện tại.");

        // Bất kể vai trò: không cho chuyển tới Introduced trở lên nếu sinh viên chưa có doanh nghiệp.
        // Việc gắn CompanyId phải đi qua duyệt/gán yêu cầu (PlacementRequestService / AssignCompanyDirectAsync).
        if (toStatus != StudentStatus.NoCompany && student.CompanyId is null)
            return (false, "Sinh viên chưa có doanh nghiệp — hãy duyệt yêu cầu hoặc gán doanh nghiệp trước.");

        // "lùi" = giá trị enum đích nhỏ hơn giá trị hiện tại (xem thứ tự khai báo StudentStatus)
        var isBackward = (int)toStatus < (int)fromStatus;

        if (!isAdmin)
        {
            // Sinh viên chỉ được đi đúng 1 bước, đúng theo bộ chuyển đổi cho phép ở mục 5.3 PROJECT_CONTEXT.
            var allowedForward =
                (fromStatus == StudentStatus.Introduced && toStatus == StudentStatus.Interviewed) ||
                (fromStatus == StudentStatus.Accepted && toStatus == StudentStatus.Interning);

            var allowedBackward =
                (fromStatus == StudentStatus.Introduced && toStatus == StudentStatus.NoCompany) ||
                (fromStatus == StudentStatus.Interviewed && toStatus == StudentStatus.Introduced);

            if (!allowedForward && !allowedBackward)
                return (false, "Bạn không có quyền chuyển sang trạng thái này.");
        }

        if (isBackward && string.IsNullOrWhiteSpace(dto.Note))
            return (false, "Phải nhập Note (lý do) khi lùi trạng thái.");

        student.Status = toStatus;
        student.UpdatedAt = DateTime.UtcNow;

        if (toStatus == StudentStatus.NoCompany)
        {
            student.CompanyId = null;
            student.JobPositionId = null;
        }

        // StatusHistory thật chỉ có cột Note, không có RejectReason riêng —
        // gộp RejectReason vào Note khi lưu để không mất thông tin.
        var noteToSave = string.IsNullOrWhiteSpace(dto.RejectReason)
            ? dto.Note
            : string.IsNullOrWhiteSpace(dto.Note)
                ? $"Lý do từ chối: {dto.RejectReason}"
                : $"{dto.Note} | Lý do từ chối: {dto.RejectReason}";

        _db.StatusHistories.Add(new StatusHistory
        {
            StudentId = student.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            CompanyId = student.CompanyId,
            Note = noteToSave,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedByUserId
        });

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ---------- 6. Lịch sử trạng thái ----------

    public async Task<List<StatusHistoryDto>?> GetStatusHistoryAsync(int studentId)
    {
        var exists = await _db.Students.AnyAsync(s => s.Id == studentId);
        if (!exists) return null;

        return await _db.StatusHistories
            .Where(h => h.StudentId == studentId)
            .Include(h => h.ChangedByUser) // giả định navigation StatusHistory.ChangedByUser -> User
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new StatusHistoryDto
            {
                Id = h.Id,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                CompanyId = h.CompanyId,
                Note = h.Note,
                ChangedAt = h.ChangedAt,
                ChangedByUsername = h.ChangedByUser != null ? h.ChangedByUser.Username : string.Empty
            })
            .ToListAsync();
    }

    // ---------- 7. Admin gán doanh nghiệp trực tiếp ----------

    public async Task<(bool Success, string? Error)> AssignCompanyDirectAsync(int studentId, AssignCompanyDto dto, int adminUserId)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        if (student is null)
            return (false, "Không tìm thấy sinh viên.");

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == dto.CompanyId);
        if (company is null)
            return (false, "Không tìm thấy doanh nghiệp.");

        if (dto.JobPositionId.HasValue)
        {
            var jobPosition = await _db.JobPositions.FirstOrDefaultAsync(jp => jp.Id == dto.JobPositionId.Value);
            if (jobPosition is null)
                return (false, "Không tìm thấy vị trí tuyển dụng.");

            if (jobPosition.CompanyId != dto.CompanyId)
                return (false, "Vị trí tuyển dụng không thuộc doanh nghiệp đã chọn.");
        }

        // Nếu còn PlacementRequest đang chờ duyệt của sinh viên này -> huỷ để tránh xung đột dữ liệu.
        var pendingRequest = await _db.PlacementRequests
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.Status == RequestStatus.Pending);
        if (pendingRequest is not null)
        {
            pendingRequest.Status = RequestStatus.Rejected;
            pendingRequest.RejectReason = "Admin đã gán doanh nghiệp trực tiếp.";
            pendingRequest.ReviewedBy = adminUserId;
            pendingRequest.ReviewedAt = DateTime.UtcNow;
        }

        var fromStatus = student.Status;

        student.CompanyId = dto.CompanyId;
        student.JobPositionId = dto.JobPositionId;
        student.Status = StudentStatus.Introduced;
        student.UpdatedAt = DateTime.UtcNow;

        _db.StatusHistories.Add(new StatusHistory
        {
            StudentId = student.Id,
            FromStatus = fromStatus,
            ToStatus = StudentStatus.Introduced,
            CompanyId = dto.CompanyId,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? "Admin gán doanh nghiệp trực tiếp." : dto.Note,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = adminUserId
        });

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ---------- 8. Xuất danh sách sinh viên (Excel/CSV) ----------

    // Nhãn tiếng Việt cho StudentStatus — chưa có sẵn ở đâu khác trong project (đã kiểm tra
    // frontend/backend), nên khai báo dùng chung tại đây cho việc export.
    private static readonly Dictionary<StudentStatus, string> StatusLabels = new()
    {
        [StudentStatus.NoCompany] = "Chưa có doanh nghiệp",
        [StudentStatus.Introduced] = "Đã giới thiệu",
        [StudentStatus.Interviewed] = "Đã phỏng vấn",
        [StudentStatus.Accepted] = "Đã nhận",
        [StudentStatus.Interning] = "Đang thực tập",
        [StudentStatus.Completed] = "Hoàn thành"
    };

    private static readonly string[] ExportHeaders =
    {
        "MSSV", "Họ tên", "Lớp", "Ngành", "Email", "Số điện thoại",
        "Trạng thái", "Doanh nghiệp", "Vị trí", "Ngày tạo tài khoản"
    };

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportStudentsAsync(StudentFilterDto filter, string format)
    {
        // Global query filter (!IsDeleted) đã tự động áp dụng, không cần lọc tay thêm.
        var query = _db.Students
            .Include(s => s.Company)
            .Include(s => s.JobPosition)
            .Include(s => s.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(s =>
                s.StudentCode.ToLower().Contains(search) ||
                s.FullName.ToLower().Contains(search));
        }

        if (filter.Status.HasValue)
            query = query.Where(s => s.Status == filter.Status.Value);

        if (filter.CompanyId.HasValue)
            query = query.Where(s => s.CompanyId == filter.CompanyId.Value);

        var students = await query
            .OrderBy(s => s.StudentCode)
            .ToListAsync();

        var rows = students.Select(s => new[]
        {
            s.StudentCode,
            s.FullName,
            s.ClassName,
            s.Major,
            s.Email,
            s.PhoneNumber,
            StatusLabels.TryGetValue(s.Status, out var label) ? label : s.Status.ToString(),
            s.Company?.Name ?? string.Empty,
            s.JobPosition?.Title ?? string.Empty,
            s.User is not null ? s.User.CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : string.Empty
        }).ToList();

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return format.ToLowerInvariant() switch
        {
            "xlsx" => (
                BuildExportXlsx(rows),
                $"DanhSachSinhVien_{timestamp}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            "csv" => (
                BuildExportCsv(rows),
                $"DanhSachSinhVien_{timestamp}.csv",
                "text/csv"),
            _ => throw new InvalidOperationException("Định dạng không hợp lệ. Chỉ hỗ trợ 'csv' hoặc 'xlsx'.")
        };
    }

    private static byte[] BuildExportXlsx(List<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Danh sách sinh viên");

        for (int c = 0; c < ExportHeaders.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = ExportHeaders[c];
            cell.Style.Font.Bold = true;
        }

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (int c = 0; c < row.Length; c++)
                ws.Cell(r + 2, c + 1).Value = row[c];
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // Đồng bộ pattern EscapeCsv với ExportAccountsCsv ở StudentsController (Giai đoạn 3).
    private static byte[] BuildExportCsv(List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", ExportHeaders.Select(EscapeCsv)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));

        // UTF-8 có BOM để Excel mở tiếng Việt không lỗi font.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsv(string value)
    {
        value ??= string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // ---------- mapping tay Entity -> DTO ----------

    private static StudentDto MapToDto(Student s) => new()
    {
        Id = s.Id,
        StudentCode = s.StudentCode,
        FullName = s.FullName,
        Email = s.Email,
        PhoneNumber = s.PhoneNumber,
        Major = s.Major,
        ClassName = s.ClassName,
        Status = s.Status,
        CompanyId = s.CompanyId,
        CompanyName = s.Company != null ? s.Company.Name : null,
        JobPositionId = s.JobPositionId,
        JobPositionTitle = s.JobPosition != null ? s.JobPosition.Title : null,
        HasAccount = s.User != null,
        CreatedAt = s.CreatedAt
    };
}
