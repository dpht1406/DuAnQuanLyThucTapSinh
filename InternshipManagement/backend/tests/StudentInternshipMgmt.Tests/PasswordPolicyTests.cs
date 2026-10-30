using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Auth.Dtos;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("abcdefgh")]
    [InlineData("12345678")]
    [InlineData("        ")]
    [InlineData("Abc1234\n")]
    public void Change_password_validator_rejects_invalid_password(string password)
    {
        var result = new ChangePasswordRequestDtoValidator().Validate(new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass2025",
            NewPassword = password
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequestDto.NewPassword));
    }

    [Fact]
    public void Change_password_validator_rejects_password_longer_than_128_characters()
    {
        var result = new ChangePasswordRequestDtoValidator().Validate(new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass2025",
            NewPassword = $"A1{new string('b', 127)}"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequestDto.NewPassword));
    }

    [Theory]
    [InlineData("Matkhau2026")]
    [InlineData("Abc12345")]
    public void Change_password_validator_accepts_valid_password(string password)
    {
        var result = new ChangePasswordRequestDtoValidator().Validate(new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass2025",
            NewPassword = password
        });

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequestDto.NewPassword));
    }

    [Fact]
    public void Change_password_validator_accepts_128_character_password()
    {
        var result = new ChangePasswordRequestDtoValidator().Validate(new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass2025",
            NewPassword = $"A1{new string('b', 126)}"
        });

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequestDto.NewPassword));
    }

    [Fact]
    public void Change_password_validator_rejects_empty_current_password()
    {
        var result = new ChangePasswordRequestDtoValidator().Validate(new ChangePasswordRequestDto
        {
            CurrentPassword = "",
            NewPassword = "Matkhau2026"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequestDto.CurrentPassword));
    }

    [Fact]
    public void Change_password_validator_rejects_new_password_equal_to_current_password()
    {
        var result = new ChangePasswordRequestDtoValidator().Validate(new ChangePasswordRequestDto
        {
            CurrentPassword = "Matkhau2026",
            NewPassword = "Matkhau2026"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequestDto.NewPassword));
    }

    [Fact]
    public void Login_validator_only_requires_username_and_password()
    {
        var result = new LoginRequestDtoValidator().Validate(new LoginRequestDto
        {
            Username = "user",
            Password = "weak"
        });

        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "weak")]
    [InlineData("user", "")]
    [InlineData("   ", "   ")]
    public void Login_validator_rejects_empty_username_or_password(string username, string password)
    {
        var result = new LoginRequestDtoValidator().Validate(new LoginRequestDto
        {
            Username = username,
            Password = password
        });

        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void One_hundred_generated_passwords_satisfy_policy()
    {
        for (var index = 0; index < 100; index++)
        {
            Assert.True(PasswordPolicy.TryValidate(PasswordGenerator.Generate(), out _));
        }
    }
}
