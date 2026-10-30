using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentInternshipMgmt.Application.Features.Auth;
using StudentInternshipMgmt.Application.Features.Auth.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class AuthPasswordChangeTests
{
    [Fact]
    public async Task Change_password_enforces_policy_when_called_without_a_request_validator()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ChangePasswordAsync(1, new ChangePasswordRequestDto
            {
                CurrentPassword = "OldPass2025",
                NewPassword = "abcdefgh"
            }));

        exception.Message.Should().Contain("Mật khẩu phải có");
    }

    [Fact]
    public async Task Change_password_rejects_an_empty_new_password()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "empty-password-user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass2025"),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ChangePasswordAsync(user.Id, new ChangePasswordRequestDto
            {
                CurrentPassword = "OldPass2025",
                NewPassword = string.Empty
            }));

        exception.Message.Should().Contain("không được để trống");
        BCrypt.Net.BCrypt.Verify("OldPass2025", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Changing_password_revokes_refresh_tokens_and_switches_password()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "password-change-user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass2025"),
            Role = UserRole.Admin,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var firstLogin = await service.LoginAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "OldPass2025"
        });
        var secondLogin = await service.LoginAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "OldPass2025"
        });

        await service.ChangePasswordAsync(user.Id, new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass2025",
            NewPassword = "NewPass2026"
        });

        var revokedTokens = await dbContext.RefreshTokens.ToListAsync();
        revokedTokens.Should().HaveCount(2);
        revokedTokens.Should().OnlyContain(token => token.IsRevoked && token.RevokedAt.HasValue);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = firstLogin.RefreshToken }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = secondLogin.RefreshToken }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequestDto { Username = user.Username, Password = "OldPass2025" }));

        var newLogin = await service.LoginAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "NewPass2026"
        });
        newLogin.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    private static AuthService CreateService(AppDbContext dbContext) =>
        new(dbContext, Options.Create(new JwtSettings
        {
            Issuer = "password-change-tests",
            Audience = "password-change-tests",
            SecretKey = "password-change-tests-secret-key-at-least-32-characters",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        }));

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
