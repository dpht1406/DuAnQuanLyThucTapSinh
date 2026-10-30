using FluentValidation;
using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.Auth.Dtos;

public class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty()
            .WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(request => request.NewPassword)
            .Custom((value, context) =>
            {
                if (!PasswordPolicy.TryValidate(value, out var error))
                {
                    context.AddFailure(error!);
                }
            });

        RuleFor(request => request.NewPassword)
            .NotEqual(request => request.CurrentPassword)
            .WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
    }
}

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(request => request.Username)
            .NotEmpty()
            .WithMessage("Tên đăng nhập không được để trống.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Mật khẩu không được để trống.");
    }
}
