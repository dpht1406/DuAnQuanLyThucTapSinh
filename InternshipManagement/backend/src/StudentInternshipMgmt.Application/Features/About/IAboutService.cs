namespace StudentInternshipMgmt.Application.Features.About;

public interface IAboutService
{
    Task<AboutPageDto> GetAboutAsync();
    Task<(bool Success, List<string>? Errors, AboutPageDto? Data)> UpdateAboutAsync(AboutContentDto content, int? userId);
    Task<AboutPageDto> ResetAboutAsync(int? userId);
}
