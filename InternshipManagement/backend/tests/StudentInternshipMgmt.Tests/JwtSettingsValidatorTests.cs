using StudentInternshipMgmt.Application.Features.Auth;

namespace StudentInternshipMgmt.Tests;

public class JwtSettingsValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_rejects_empty_secret_key(string? secretKey)
    {
        var settings = new JwtSettings { SecretKey = secretKey! };

        var act = () => JwtSettingsValidator.Validate(settings);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("Jwt:SecretKey", exception.Message);
    }

    [Fact]
    public void Validate_rejects_secret_key_shorter_than_32_characters()
    {
        var settings = new JwtSettings { SecretKey = new string('x', 20) };

        var act = () => JwtSettingsValidator.Validate(settings);

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("at least 32 characters", exception.Message);
        Assert.DoesNotContain(settings.SecretKey, exception.Message);
    }

    [Fact]
    public void Validate_accepts_secret_key_with_at_least_32_characters()
    {
        var settings = new JwtSettings { SecretKey = new string('x', 32) };

        var act = () => JwtSettingsValidator.Validate(settings);

        act();
    }
}
