using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Companies;
using StudentInternshipMgmt.Application.Features.Companies.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    // Khớp đúng HasMaxLength trong CompanyConfiguration — tránh DbUpdateException khi
    // dữ liệu vượt quá cột NVARCHAR, đồng thời trả lỗi rõ ràng thay vì lỗi SQL thô.
    private const int NameMaxLength = 200;
    private const int AddressMaxLength = 300;
    private const int IndustryMaxLength = 150;
    private const int ContactPersonMaxLength = 150;

    private readonly AppDbContext _db;

    public CompanyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CompanyDto>> GetCompaniesAsync(CompanyFilterDto filter)
    {
        var query = _db.Companies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Industry))
        {
            var industry = filter.Industry.Trim().ToLower();
            query = query.Where(c => c.Industry.ToLower() == industry);
        }

        // Chiếu (Select) trực tiếp sang CompanyDto ngay trong LINQ-to-Entities để EF Core
        // dịch được ra SQL — không gọi qua hàm map thủ công ở bước này.
        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                Address = c.Address,
                Industry = c.Industry,
                ContactPerson = c.ContactPerson
            })
            .ToPagedResultAsync(filter);
    }

    public async Task<CompanyDetailDto?> GetCompanyByIdAsync(int id)
    {
        var company = await _db.Companies
            .Include(c => c.JobPositions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (company is null)
            return null;

        return new CompanyDetailDto
        {
            Id = company.Id,
            Name = company.Name,
            Address = company.Address,
            Industry = company.Industry,
            ContactPerson = company.ContactPerson,
            JobPositions = company.JobPositions
                .Select(jp => new JobPositionDto
                {
                    Id = jp.Id,
                    Title = jp.Title,
                    Quantity = jp.Quantity,
                    IsOpen = jp.IsOpen
                })
                .ToList()
        };
    }

    public async Task<(bool Success, string? Error, CompanyDto? Data)> CreateCompanyAsync(CreateCompanyDto dto)
    {
        var validationError = ValidateFields(dto.Name, dto.Address, dto.Industry, dto.ContactPerson);
        if (validationError is not null)
            return (false, validationError, null);

        var company = new Company
        {
            Name = dto.Name.Trim(),
            Address = dto.Address.Trim(),
            Industry = dto.Industry.Trim(),
            ContactPerson = dto.ContactPerson.Trim()
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        return (true, null, MapToDto(company));
    }

    public async Task<(bool Success, string? Error, bool NotFound)> UpdateCompanyAsync(int id, UpdateCompanyDto dto)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
        if (company is null)
            return (false, "Không tìm thấy công ty.", true);

        var validationError = ValidateFields(dto.Name, dto.Address, dto.Industry, dto.ContactPerson);
        if (validationError is not null)
            return (false, validationError, false);

        company.Name = dto.Name.Trim();
        company.Address = dto.Address.Trim();
        company.Industry = dto.Industry.Trim();
        company.ContactPerson = dto.ContactPerson.Trim();

        await _db.SaveChangesAsync();
        return (true, null, false);
    }

    public async Task<(bool Success, string? Error, bool NotFound)> DeleteCompanyAsync(int id)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
        if (company is null)
            return (false, "Không tìm thấy công ty.", true);

        // Check chính theo đúng yêu cầu: còn Student nào (chưa xóa mềm) có CompanyId = id này.
        // Students đã có global query filter (!IsDeleted) cấu hình ở StudentConfiguration,
        // nên không cần lặp lại điều kiện đó ở đây.
        var hasActiveStudents = await _db.Students.AnyAsync(s => s.CompanyId == id);
        if (hasActiveStudents)
            return (false, "Không thể xóa công ty vì vẫn còn sinh viên đang thực tập tại công ty này.", false);

        _db.Companies.Remove(company);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Lưu ý: JobPositions.CompanyId cũng là FK OnDelete(Restrict) (xem
            // JobPositionConfiguration) — đề bài chỉ yêu cầu check qua Student, nhưng nếu
            // công ty còn JobPosition (kể cả khi không còn Student nào), SQL Server vẫn sẽ
            // chặn ở tầng DB và ném DbUpdateException. Bắt lại ở đây để không lộ lỗi 500 thô
            // ra ngoài — đúng tinh thần "trả lỗi rõ ràng" của yêu cầu 2c.
            return (false,
                "Không thể xóa công ty vì vẫn còn vị trí tuyển dụng (JobPosition) liên kết với công ty này.",
                false);
        }

        return (true, null, false);
    }

    private static string? ValidateFields(string name, string address, string industry, string contactPerson)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Tên công ty (Name) là bắt buộc.";
        if (name.Trim().Length > NameMaxLength)
            return $"Tên công ty không được quá {NameMaxLength} ký tự.";

        if (address.Trim().Length > AddressMaxLength)
            return $"Địa chỉ không được quá {AddressMaxLength} ký tự.";

        if (industry.Trim().Length > IndustryMaxLength)
            return $"Lĩnh vực (Industry) không được quá {IndustryMaxLength} ký tự.";

        if (contactPerson.Trim().Length > ContactPersonMaxLength)
            return $"Người liên hệ (ContactPerson) không được quá {ContactPersonMaxLength} ký tự.";

        return null;
    }

    private static CompanyDto MapToDto(Company c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Address = c.Address,
        Industry = c.Industry,
        ContactPerson = c.ContactPerson
    };
}
