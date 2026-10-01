using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Infrastructure.Persistence.Seed;

/// <summary>
/// Centralized seed data consumed by IEntityTypeConfiguration.HasData().
/// EF Core model-building requires deterministic values, so all keys, dates
/// and the admin password hash below are fixed constants rather than being
/// generated at runtime.
/// </summary>
public static class SeedData
{
    // Fixed point in time used for every seeded CreatedAt/UpdatedAt value so that
    // the generated migration snapshot stays stable across re-builds.
    public static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Default admin credentials for first login: username "admin", password "Admin@123".
    // The hash below was pre-computed with BCrypt (work factor 11); it must be
    // changed to a fresh hash if the default password is changed.
    public const string AdminDefaultPasswordHash =
        "$2b$11$1zB61f0oX7e98EaZGDvS8OVQumC4VlWWMXlU/yXz7v8q.Pe.dy9QW";

    public static User[] Users => new[]
    {
        new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = AdminDefaultPasswordHash,
            Role = UserRole.Admin,
            IsActive = true,
            StudentId = null,
            MustChangePassword = false,
            CreatedAt = SeedTimestamp
        }
    };

    public static Company[] Companies => new[]
    {
        new Company
        {
            Id = 1,
            Name = "Cong ty TNHH Giai phap Phan mem ABC",
            Address = "123 Nguyen Van Linh, Quan 7, TP.HCM",
            Industry = "Phat trien phan mem",
            ContactPerson = "Nguyen Van A"
        },
        new Company
        {
            Id = 2,
            Name = "Cong ty Co phan Cong nghe XYZ",
            Address = "45 Le Loi, Quan 1, TP.HCM",
            Industry = "Cong nghe thong tin",
            ContactPerson = "Tran Thi B"
        },
        new Company
        {
            Id = 3,
            Name = "Cong ty TNHH Dich vu So Delta",
            Address = "78 Vo Van Ngan, TP. Thu Duc, TP.HCM",
            Industry = "Dich vu so / Du lieu",
            ContactPerson = "Le Van C"
        }
    };

    public static JobPosition[] JobPositions => new[]
    {
        new JobPosition
        {
            Id = 1,
            CompanyId = 1,
            Title = "Thuc tap sinh Lap trinh vien .NET",
            Department = "Phòng Công nghệ",
            Deadline = new DateTime(2026, 12, 31),
            Quantity = 3,
            Description = "Thuc tap phat trien API voi ASP.NET Core va Entity Framework Core.",
            IsOpen = true
        },
        new JobPosition
        {
            Id = 2,
            CompanyId = 1,
            Title = "Thuc tap sinh Kiem thu phan mem (Tester)",
            Department = "Phòng Kiểm thử",
            Deadline = new DateTime(2026, 10, 4),
            Quantity = 2,
            Description = "Thuc tap kiem thu chuc nang va viet test case.",
            IsOpen = true
        },
        new JobPosition
        {
            Id = 3,
            CompanyId = 2,
            Title = "Thuc tap sinh Frontend ReactJS",
            Department = "Phòng Thiết kế",
            Deadline = new DateTime(2026, 9, 20),
            Quantity = 2,
            Description = "Thuc tap xay dung giao dien nguoi dung voi ReactJS.",
            IsOpen = true
        },
        new JobPosition
        {
            Id = 4,
            CompanyId = 3,
            Title = "Thuc tap sinh Phan tich du lieu",
            Department = "Phòng Dữ liệu",
            Quantity = 1,
            Description = "Thuc tap xu ly va truc quan hoa du lieu.",
            IsOpen = false
        },
        new JobPosition
        {
            Id = 1001,
            CompanyId = 2,
            Title = "Thực tập sinh Phân tích dữ liệu",
            Department = "Phòng Dữ liệu",
            Location = "Tòa nhà Công nghệ, Quận 3, TP.HCM",
            Deadline = new DateTime(2026, 10, 4),
            Quantity = 2,
            Description = "Hỗ trợ làm sạch dữ liệu và xây dựng báo cáo trực quan.",
            IsOpen = true
        },
        new JobPosition
        {
            Id = 1002,
            CompanyId = 3,
            Title = "Thực tập sinh Thiết kế sản phẩm",
            Department = "Phòng Thiết kế",
            Quantity = 1,
            Description = "Tham gia thiết kế trải nghiệm và giao diện sản phẩm số.",
            IsOpen = true
        },
        new JobPosition
        {
            Id = 1003,
            CompanyId = 1,
            Title = "Thực tập sinh Kiểm thử phần mềm",
            Department = "Phòng Kiểm thử",
            Deadline = new DateTime(2026, 12, 31),
            Quantity = 2,
            Description = "Viết và thực thi kịch bản kiểm thử cho sản phẩm.",
            IsOpen = false
        },
        new JobPosition
        {
            Id = 1004,
            CompanyId = 3,
            Title = "Thực tập sinh Phát triển .NET",
            Department = "Phòng Công nghệ",
            Deadline = new DateTime(2026, 12, 31),
            Quantity = 3,
            Description = "Phát triển dịch vụ web với ASP.NET Core.",
            IsOpen = true
        }
    };
}
