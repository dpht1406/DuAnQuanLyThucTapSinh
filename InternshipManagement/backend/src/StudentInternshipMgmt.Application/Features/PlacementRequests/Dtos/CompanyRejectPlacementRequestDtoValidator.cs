using FluentValidation;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class CompanyRejectPlacementRequestDtoValidator : AbstractValidator<CompanyRejectPlacementRequestDto>
{
    public CompanyRejectPlacementRequestDtoValidator()
    {
        RuleFor(request => request.Reason)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Lý do từ chối là bắt buộc.")
            .Must(value => value.Trim().Length <= 1000)
            .WithMessage("Lý do từ chối không được vượt quá 1000 ký tự.");
    }
}
