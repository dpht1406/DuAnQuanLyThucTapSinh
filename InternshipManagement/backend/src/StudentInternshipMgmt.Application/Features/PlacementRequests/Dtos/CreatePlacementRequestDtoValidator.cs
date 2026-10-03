using FluentValidation;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Students;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class CreatePlacementRequestDtoValidator : AbstractValidator<CreatePlacementRequestDto>
{
    public CreatePlacementRequestDtoValidator()
    {
        RuleFor(request => request.CompanyId)
            .GreaterThan(0)
            .WithMessage("Doanh nghiệp ứng tuyển chưa hợp lệ.");

        RuleFor(request => request.JobPositionId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Vui lòng chọn vị trí ứng tuyển.")
            .GreaterThan(0)
            .WithMessage("Vui lòng chọn vị trí ứng tuyển.");

        RuleFor(request => request.ApplicantFullName)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Họ tên là bắt buộc.")
            .Must(value => HasTrimmedLength(value, 2, 100))
            .WithMessage("Họ tên phải từ 2 đến 100 ký tự.");

        RuleFor(request => request.ApplicantEmail)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Email ứng viên là bắt buộc.")
            .Must(value => value!.Trim().Length <= 254)
            .WithMessage("Email ứng viên không được vượt quá 254 ký tự.")
            .Must(EmailAddressValidator.IsValid)
            .WithMessage(request => EmailAddressValidator.GetErrorMessage(request.ApplicantEmail));

        RuleFor(request => request.ApplicantPhone)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Số điện thoại ứng viên là bắt buộc.")
            .Must(value => System.Text.RegularExpressions.Regex.IsMatch(
                value!.Trim(), StudentValidationRules.PhoneRegex))
            .WithMessage("Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.");

        RuleFor(request => request.ApplicantSchool)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Trường học là bắt buộc.")
            .Must(value => HasTrimmedLength(value, 2, 150))
            .WithMessage("Trường học phải từ 2 đến 150 ký tự.");

        RuleFor(request => request.ApplicantMajor)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Ngành học là bắt buộc.")
            .Must(value => HasTrimmedLength(value, 2, 150))
            .WithMessage("Ngành học phải từ 2 đến 150 ký tự.");

        RuleFor(request => request.CvUrl)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Liên kết CV là bắt buộc.")
            .Must(value => value!.Trim().Length <= 500)
            .WithMessage("Liên kết CV không được vượt quá 500 ký tự.")
            .Must(IsAllowedCvUrl)
            .WithMessage("Liên kết CV phải là URL tuyệt đối bắt đầu bằng http:// hoặc https:// và không chứa khoảng trắng.");

        RuleFor(request => request.CoverLetter)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Lời giới thiệu là bắt buộc.")
            .Must(value => HasTrimmedLength(value, 20, 2000))
            .WithMessage("Lời giới thiệu phải từ 20 đến 2000 ký tự.");
    }

    private static bool HasTrimmedLength(string? value, int minimum, int maximum)
    {
        if (value is null)
            return false;

        var length = value.Trim().Length;
        return length >= minimum && length <= maximum;
    }

    private static bool IsAllowedCvUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
            return false;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

}