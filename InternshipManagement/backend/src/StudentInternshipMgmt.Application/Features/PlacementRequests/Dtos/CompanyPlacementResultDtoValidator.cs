using FluentValidation;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

public sealed class CompanyPlacementResultDtoValidator : AbstractValidator<CompanyPlacementResultDto>
{
    public CompanyPlacementResultDtoValidator()
    {
        RuleFor(result => result.Note)
            .Must(note => note is null || note.Trim().Length <= 1000)
            .WithMessage("Ghi chú kết quả không được vượt quá 1000 ký tự.");

        RuleFor(result => result.Note)
            .Must((result, note) => result.IsPassed || !string.IsNullOrWhiteSpace(note))
            .WithMessage("Ghi chú là bắt buộc khi sinh viên không đạt.");
    }
}
