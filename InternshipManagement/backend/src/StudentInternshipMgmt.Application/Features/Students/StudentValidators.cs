using FluentValidation;
using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Students;

// Số điện thoại VN: đúng 10 chữ số, bắt đầu bằng 0.
public static class StudentValidationRules
{
    public const string PhoneRegex = @"^0[0-9]{9}$";
}

public class CreateStudentDtoValidator : AbstractValidator<CreateStudentDto>
{
    public CreateStudentDtoValidator()
    {
        RuleFor(x => x.StudentCode)
            .NotEmpty().WithMessage("StudentCode là bắt buộc.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName là bắt buộc.");

        RuleFor(x => x.Major)
            .NotEmpty().WithMessage("Major là bắt buộc.");

        RuleFor(x => x.ClassName)
            .NotEmpty().WithMessage("ClassName là bắt buộc.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .Must(EmailAddressValidator.IsValid)
            .WithMessage(x => EmailAddressValidator.GetErrorMessage(x.Email));

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("PhoneNumber là bắt buộc.")
            .Matches(StudentValidationRules.PhoneRegex)
            .WithMessage("PhoneNumber phải là số Việt Nam hợp lệ (10 số, bắt đầu bằng 0).");
    }
}

public class UpdateStudentDtoValidator : AbstractValidator<UpdateStudentDto>
{
    public UpdateStudentDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName là bắt buộc.");

        RuleFor(x => x.Major)
            .NotEmpty().WithMessage("Major là bắt buộc.");

        RuleFor(x => x.ClassName)
            .NotEmpty().WithMessage("ClassName là bắt buộc.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .Must(EmailAddressValidator.IsValid)
            .WithMessage(x => EmailAddressValidator.GetErrorMessage(x.Email));

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("PhoneNumber là bắt buộc.")
            .Matches(StudentValidationRules.PhoneRegex)
            .WithMessage("PhoneNumber phải là số Việt Nam hợp lệ (10 số, bắt đầu bằng 0).");
    }
}
