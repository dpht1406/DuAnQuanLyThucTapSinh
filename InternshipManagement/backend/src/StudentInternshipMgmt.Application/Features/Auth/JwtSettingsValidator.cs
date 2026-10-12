namespace StudentInternshipMgmt.Application.Features.Auth;

public static class JwtSettingsValidator
{
    public static void Validate(JwtSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.SecretKey) || settings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey must be configured with at least 32 characters.");
        }
    }
}
