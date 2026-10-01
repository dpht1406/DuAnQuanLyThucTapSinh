using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class StudentPasswordResetTests
{
    [Fact]
    public async Task ResetPasswordAsync_changes_hash_and_requires_password_change()
    {
        await using var dbContext = CreateDbContext();
        var (student, user) = await SeedStudentAndUser(dbContext);
        var oldHash = user.PasswordHash;
        var service = CreateService(dbContext);

        var result = await service.ResetPasswordAsync(student.Id, adminUserId: 90);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.StudentCode.Should().Be(student.StudentCode);
        user.PasswordHash.Should().NotBe(oldHash);
        BCrypt.Net.BCrypt.Verify(result.Data.TemporaryPassword, user.PasswordHash).Should().BeTrue();
        user.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_revokes_all_unrevoked_refresh_tokens()
    {
        await using var dbContext = CreateDbContext();
        var (student, user) = await SeedStudentAndUser(dbContext);
        var activeTokens = new[]
        {
            new RefreshToken { UserId = user.Id, Token = "refresh-1", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { UserId = user.Id, Token = "refresh-2", ExpiresAt = DateTime.UtcNow.AddDays(2) }
        };
        var revokedToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "refresh-revoked",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        };
        dbContext.RefreshTokens.AddRange(activeTokens);
        dbContext.RefreshTokens.Add(revokedToken);
        await dbContext.SaveChangesAsync();

        await CreateService(dbContext).ResetPasswordAsync(student.Id, adminUserId: 90);

        activeTokens.Should().OnlyContain(token => token.IsRevoked && token.RevokedAt.HasValue);
        revokedToken.RevokedAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ResetPasswordAsync_does_not_activate_inactive_user()
    {
        await using var dbContext = CreateDbContext();
        var (student, user) = await SeedStudentAndUser(dbContext, isActive: false);

        var result = await CreateService(dbContext).ResetPasswordAsync(student.Id, adminUserId: 90);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_returns_not_found_for_missing_student()
    {
        await using var dbContext = CreateDbContext();

        var result = await CreateService(dbContext).ResetPasswordAsync(studentId: 404, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Không tìm thấy sinh viên.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_returns_error_when_student_has_no_account()
    {
        await using var dbContext = CreateDbContext();
        var student = CreateStudent();
        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).ResetPasswordAsync(student.Id, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Sinh viên này chưa có tài khoản.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_does_not_reset_admin_user_linked_to_student()
    {
        await using var dbContext = CreateDbContext();
        var student = CreateStudent();
        var adminUser = new User
        {
            Username = "admin-linked",
            PasswordHash = "existing-hash",
            Role = UserRole.Admin,
            Student = student,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.AddRange(student, adminUser);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).ResetPasswordAsync(student.Id, adminUserId: 90);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Sinh viên này chưa có tài khoản.");
        adminUser.PasswordHash.Should().Be("existing-hash");
    }

    [Fact]
    public void Generate_returns_ten_characters_with_uppercase_lowercase_and_digit()
    {
        var password = PasswordGenerator.Generate();

        password.Should().HaveLength(10);
        password.Any(char.IsUpper).Should().BeTrue();
        password.Any(char.IsLower).Should().BeTrue();
        password.Any(char.IsDigit).Should().BeTrue();
    }

    private static StudentService CreateService(AppDbContext dbContext) =>
        new(dbContext, NullLogger<StudentService>.Instance);

    private static async Task<(Student Student, User User)> SeedStudentAndUser(
        AppDbContext dbContext,
        bool isActive = true)
    {
        var student = CreateStudent();
        var user = new User
        {
            Username = student.StudentCode,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password"),
            Role = UserRole.User,
            MustChangePassword = false,
            IsActive = isActive,
            Student = student,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.AddRange(student, user);
        await dbContext.SaveChangesAsync();
        return (student, user);
    }

    private static Student CreateStudent() => new()
    {
        StudentCode = $"SV{Guid.NewGuid():N}"[..8],
        FullName = "Sinh viên thử nghiệm",
        Email = "student@example.com",
        Major = "Công nghệ",
        ClassName = "SE01",
        Status = StudentStatus.NoCompany,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}