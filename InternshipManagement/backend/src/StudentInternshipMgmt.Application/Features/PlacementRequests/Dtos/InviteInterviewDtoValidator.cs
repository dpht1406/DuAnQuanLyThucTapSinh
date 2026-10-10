using FluentValidation;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public class InviteInterviewDtoValidator : AbstractValidator<InviteInterviewDto>
{
    public InviteInterviewDtoValidator()
    {
        RuleFor(request => request.InterviewAt)
            .Must(IsInFuture)
            .WithMessage("Thời gian phỏng vấn phải ở tương lai.");

        RuleFor(request => request.InterviewLocation)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Địa điểm phỏng vấn là bắt buộc.")
            .Must(value => value.Trim().Length <= 500)
            .WithMessage("Địa điểm phỏng vấn không được vượt quá 500 ký tự.");

        RuleFor(request => request.InterviewNote)
            .Must(value => value is null || value.Trim().Length <= 1000)
            .WithMessage("Ghi chú phỏng vấn không được vượt quá 1000 ký tự.");
    }

    private static bool IsInFuture(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return utcValue > DateTime.UtcNow;
    }
}
