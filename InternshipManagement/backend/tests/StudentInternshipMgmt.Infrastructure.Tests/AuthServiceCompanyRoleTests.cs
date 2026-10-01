using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

public class AuthServiceCompanyRoleTests
{
    [Fact]
    public async Task Login_company_user_includes_company_claim_and_company_role()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany();
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        var user = CreateUser("company-user", UserRole.Company);
        user.CompanyId = company.Id;
        user.Company = company;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var response = await CreateService(dbContext).LoginAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "test-password"
        });
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        token.Claims.Should().Contain(claim =>
            claim.Type == "CompanyId" && claim.Value == company.Id.ToString());
        token.Claims.Should().Contain(claim =>
            claim.Type == "role" && claim.Value == UserRole.Company.ToString());
        token.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.Role && claim.Value == UserRole.Company.ToString());
        token.Claims.Should().NotContain(claim => claim.Type == "StudentId");
    }

    [Fact]
    public async Task Login_student_user_includes_student_claim_without_company_claim()
    {
        await using var dbContext = CreateDbContext();
        var student = CreateStudent();
        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync();

        var user = CreateUser("student-user", UserRole.User);
        user.StudentId = student.Id;
        user.Student = student;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var response = await CreateService(dbContext).LoginAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "test-password"
        });
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        token.Claims.Should().Contain(claim =>
            claim.Type == "StudentId" && claim.Value == student.Id.ToString());
        token.Claims.Should().NotContain(claim => claim.Type == "CompanyId");
    }

    [Fact]
    public async Task GetMe_company_user_returns_company_role_and_company_id()
    {
        await using var dbContext = CreateDbContext();
        var company = CreateCompany();
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        var user = CreateUser("company-me", UserRole.Company);
        user.CompanyId = company.Id;
        user.Company = company;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var response = await CreateService(dbContext).GetMeAsync(user.Id);

        response.Role.Should().Be(UserRole.Company.ToString());
        response.CompanyId.Should().Be(company.Id);
        response.StudentId.Should().BeNull();
    }

    [Fact]
    public async Task GetMe_student_user_returns_null_company_id()
    {
        await using var dbContext = CreateDbContext();
        var student = CreateStudent();
        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync();

        var user = CreateUser("student-me", UserRole.User);
        user.StudentId = student.Id;
        user.Student = student;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var response = await CreateService(dbContext).GetMeAsync(user.Id);

        response.CompanyId.Should().BeNull();
    }

    private static AuthService CreateService(AppDbContext dbContext) =>
        new(dbContext, Options.Create(new JwtSettings
        {
            Issuer = "company-role-tests",
            Audience = "company-role-tests",
            SecretKey = "company-role-tests-secret-key-at-least-32-characters",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        }));

    private static User CreateUser(string username, UserRole role) => new()
    {
        Username = username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("test-password"),
        Role = role,
        IsActive = true,
        MustChangePassword = false,
        CreatedAt = DateTime.UtcNow
    };

    private static Company CreateCompany() => new()
    {
        Name = "Example Company",
        Address = "Example Address",
        Industry = "Technology",
        ContactPerson = "Example Contact"
    };

    private static Student CreateStudent() => new()
    {
        StudentCode = $"SV{Guid.NewGuid():N}"[..8],
        FullName = "Test Student",
        Email = "student@example.com",
        Major = "Technology",
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