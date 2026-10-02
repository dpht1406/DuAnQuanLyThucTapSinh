using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

namespace StudentInternshipMgmt.Tests;

public class CreatePlacementRequestDtoValidatorTests
{
    [Fact]
    public void Validate_with_complete_application_has_no_errors()
    {
        var result = new CreatePlacementRequestDtoValidator().Validate(CreateValidDto());

        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantFullName), "")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantFullName), "   ")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantEmail), "")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantEmail), "   ")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantPhone), "")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantPhone), "   ")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantSchool), "")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantSchool), "   ")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantMajor), "")]
    [InlineData(nameof(CreatePlacementRequestDto.ApplicantMajor), "   ")]
    [InlineData(nameof(CreatePlacementRequestDto.CvUrl), "")]
    [InlineData(nameof(CreatePlacementRequestDto.CvUrl), "   ")]
    [InlineData(nameof(CreatePlacementRequestDto.CoverLetter), "")]
    [InlineData(nameof(CreatePlacementRequestDto.CoverLetter), "   ")]
    public void Validate_with_required_field_empty_reports_that_field(string field, string value)
    {
        var dto = CreateValidDto();
        SetStringField(dto, field, value);

        var result = new CreatePlacementRequestDtoValidator().Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == field);
    }

    public static IEnumerable<object[]> InvalidApplicationCases()
    {
        yield return [nameof(CreatePlacementRequestDto.ApplicantFullName), "A"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantFullName), new string('A', 101)];
        yield return [nameof(CreatePlacementRequestDto.ApplicantEmail), "abc"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantEmail), "a@"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantEmail), "a b@c.com"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantEmail), $"{new string('a', 243)}@example.com"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantPhone), "0123"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantPhone), "1234567890"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantPhone), "012345678a"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantPhone), "01234567890"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), "javascript:alert(1)"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), "data:text/html,x"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), "ftp://x.com/cv"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), "file:///c:/cv.pdf"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), "not a url"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), "https://a.com/cv file.pdf"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), $"https://x.com/{new string('x', 487)}"];
        yield return [nameof(CreatePlacementRequestDto.CoverLetter), new string('x', 19)];
        yield return [nameof(CreatePlacementRequestDto.CoverLetter), new string('x', 2001)];
    }

    [Theory]
    [MemberData(nameof(InvalidApplicationCases))]
    public void Validate_rejects_invalid_field_value(string field, string value)
    {
        var dto = CreateValidDto();
        SetStringField(dto, field, value);

        var result = new CreatePlacementRequestDtoValidator().Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == field);
    }

    public static IEnumerable<object[]> ValidBoundaryCases()
    {
        yield return [nameof(CreatePlacementRequestDto.ApplicantFullName), "AB"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantFullName), new string('A', 100)];
        yield return [nameof(CreatePlacementRequestDto.ApplicantSchool), "AB"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantSchool), new string('A', 150)];
        yield return [nameof(CreatePlacementRequestDto.ApplicantMajor), "AB"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantMajor), new string('A', 150)];
        yield return [nameof(CreatePlacementRequestDto.ApplicantEmail), $"{new string('a', 242)}@example.com"];
        yield return [nameof(CreatePlacementRequestDto.ApplicantPhone), "0901234567"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), "http://x.com/cv"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), "https://drive.google.com/file/d/abc/view"];
        yield return [nameof(CreatePlacementRequestDto.CvUrl), $"https://x.com/{new string('x', 486)}"];
        yield return [nameof(CreatePlacementRequestDto.CoverLetter), new string('x', 20)];
        yield return [nameof(CreatePlacementRequestDto.CoverLetter), new string('x', 2000)];
    }

    [Theory]
    [MemberData(nameof(ValidBoundaryCases))]
    public void Validate_accepts_valid_boundary_value(string field, string value)
    {
        var dto = CreateValidDto();
        SetStringField(dto, field, value);

        var result = new CreatePlacementRequestDtoValidator().Validate(dto);

        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Validate_rejects_missing_or_zero_job_position_id(int? jobPositionId)
    {
        var dto = CreateValidDto();
        dto.JobPositionId = jobPositionId;

        var result = new CreatePlacementRequestDtoValidator().Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(dto.JobPositionId));
    }

    private static CreatePlacementRequestDto CreateValidDto() => new()
    {
        CompanyId = 1,
        JobPositionId = 1,
        ApplicantFullName = "Nguyễn Văn A",
        ApplicantEmail = "a@example.com",
        ApplicantPhone = "0901234567",
        ApplicantSchool = "Đại học thử nghiệm",
        ApplicantMajor = "Công nghệ thông tin",
        CvUrl = "https://example.com/cv.pdf",
        CoverLetter = "Tôi mong muốn được tham gia thực tập."
    };

    private static void SetStringField(CreatePlacementRequestDto dto, string field, string value)
    {
        switch (field)
        {
            case nameof(dto.ApplicantFullName): dto.ApplicantFullName = value; break;
            case nameof(dto.ApplicantEmail): dto.ApplicantEmail = value; break;
            case nameof(dto.ApplicantPhone): dto.ApplicantPhone = value; break;
            case nameof(dto.ApplicantSchool): dto.ApplicantSchool = value; break;
            case nameof(dto.ApplicantMajor): dto.ApplicantMajor = value; break;
            case nameof(dto.CvUrl): dto.CvUrl = value; break;
            case nameof(dto.CoverLetter): dto.CoverLetter = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    }
}