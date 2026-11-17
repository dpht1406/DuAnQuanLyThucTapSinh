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
            .NotEmpty().WithMessage("Mã số sinh viên không được để trống.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.");

        RuleFor(x => x.Major)
            .NotEmpty().WithMessage("Ngành không được để trống.");

        RuleFor(x => x.ClassName)
            .NotEmpty().WithMessage("Lớp không được để trống.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email không được để trống.")
            .Must(EmailAddressValidator.IsValid)
            .WithMessage(x => EmailAddressValidator.GetErrorMessage(x.Email));

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(StudentValidationRules.PhoneRegex)
            .WithMessage("Số điện thoại phải là số Việt Nam hợp lệ (10 số, bắt đầu bằng 0).");
    }
}

public class UpdateStudentDtoValidator : AbstractValidator<UpdateStudentDto>
{
    public UpdateStudentDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.");

        RuleFor(x => x.Major)
            .NotEmpty().WithMessage("Ngành không được để trống.");

        RuleFor(x => x.ClassName)
            .NotEmpty().WithMessage("Lớp không được để trống.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email không được để trống.")
            .Must(EmailAddressValidator.IsValid)
            .WithMessage(x => EmailAddressValidator.GetErrorMessage(x.Email));

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(StudentValidationRules.PhoneRegex)
            .WithMessage("Số điện thoại phải là số Việt Nam hợp lệ (10 số, bắt đầu bằng 0).");
    }
}
