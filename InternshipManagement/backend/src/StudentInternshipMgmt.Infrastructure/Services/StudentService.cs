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

    public async Task<(bool Success, string? Error)> ChangeStatusAsync(int studentId, ChangeStatusDto dto, int adminUserId)
    {
        if (!Enum.IsDefined(typeof(StudentStatus), dto.NewStatus))
            return (false, "NewStatus không hợp lệ.");

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        if (student is null)
            return (false, "Không tìm thấy sinh viên.");

        var fromStatus = student.Status;
        var toStatus = dto.NewStatus;

        // "lùi" = giá trị enum đích nhỏ hơn giá trị hiện tại (xem thứ tự khai báo StudentStatus)
        var isBackward = (int)toStatus < (int)fromStatus;

        if (isBackward && string.IsNullOrWhiteSpace(dto.Note))
            return (false, "Phải nhập Note (lý do) khi lùi trạng thái.");

        student.Status = toStatus;

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
            ChangedBy = adminUserId
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
