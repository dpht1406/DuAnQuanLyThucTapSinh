using StudentInternshipMgmt.Application.Features.Students;

namespace StudentInternshipMgmt.Tests;

public class StudentValidatorTests
{
    [Fact]
    public void Create_student_validator_rejects_whitespace_required_fields()
    {
        var dto = CreateStudent();
        dto.FullName = "   ";

        var result = new CreateStudentDtoValidator().Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(dto.FullName));
    }

    [Theory]
    [InlineData("phat@gmial.com")]
    [InlineData("student@")]
    [InlineData("student @example.com")]
    public void Create_student_validator_rejects_mistyped_email(string email)
    {
        var dto = CreateStudent();
        dto.Email = email;

        var result = new CreateStudentDtoValidator().Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(dto.Email));
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("090123456")]
    [InlineData("09012345678")]
    [InlineData("09a1234567")]
    public void Create_student_validator_rejects_invalid_phone_number(string phoneNumber)
    {
        var dto = CreateStudent();
        dto.PhoneNumber = phoneNumber;

        var result = new CreateStudentDtoValidator().Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(dto.PhoneNumber));
    }

    [Fact]
    public void Update_student_validator_rejects_whitespace_required_fields()
    {
        var dto = new UpdateStudentDto
        {
            FullName = "Student",
            Major = "Information Technology",
            ClassName = "IT-01",
            Email = "student@example.test",
            PhoneNumber = "0900000000"
        };
        dto.ClassName = "   ";

        var result = new UpdateStudentDtoValidator().Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(dto.ClassName));
    }

    private static CreateStudentDto CreateStudent() => new()
    {
        StudentCode = "SV001",
        FullName = "Student One",
        Major = "Information Technology",
        ClassName = "IT-01",
        Email = "student@example.test",
        PhoneNumber = "0900000000"
    };
}
