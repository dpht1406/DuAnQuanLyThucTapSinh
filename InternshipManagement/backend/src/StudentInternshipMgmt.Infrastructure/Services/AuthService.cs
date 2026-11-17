using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Auth;
using StudentInternshipMgmt.Application.Features.Auth.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const string DummyPasswordHash = "$2a$11$yS7jFq582gbmdJ8oFHzyJOEMLZKvWPctSMfZY1ZzCaSS4YHUqHBhm";

    private readonly AppDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext dbContext,
        IOptions<JwtSettings> jwtOptions,
        ILogger<AuthService>? logger = null)
    {
        _dbContext = dbContext;
        _jwtSettings = jwtOptions.Value;
        _logger = logger ?? NullLogger<AuthService>.Instance;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        var passwordMatches = user is null
            ? BCrypt.Net.BCrypt.Verify(request.Password, DummyPasswordHash)
            : BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        var usernameHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Username)))[..8];

        if (user is null || !passwordMatches)
        {
            _logger.LogWarning("Login failed for username hash {UsernameHash}", usernameHash);
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed for inactive username hash {UsernameHash}", usernameHash);
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa.");
        }

        var response = await IssueTokenPairAsync(user, cancellationToken);
        _logger.LogInformation("Login succeeded for user {UserId} with role {Role}", user.Id, user.Role);
        return response;
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        if (!refreshToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa.");
        }

        Revoke(refreshToken);

        return await IssueTokenPairAsync(refreshToken.User, cancellationToken);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!PasswordPolicy.TryValidate(request.NewPassword, out var passwordError))
        {
            throw new ArgumentException(passwordError);
        }

        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;

        var activeRefreshTokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && !token.IsRevoked)
            .ToListAsync(cancellationToken);
        var revokedAt = DateTime.UtcNow;
        foreach (var token in activeRefreshTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = revokedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (token is not null && !token.IsRevoked)
        {
            Revoke(token);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MeResponseDto> GetMeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        }

        return new MeResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role.ToString(),
            StudentId = user.StudentId,
            CompanyId = user.CompanyId,
            MustChangePassword = user.MustChangePassword
        };
    }

    private async Task<LoginResponseDto> IssueTokenPairAsync(User user, CancellationToken cancellationToken)
    {
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = GenerateAccessToken(user),
            RefreshToken = refreshToken.Token,
            MustChangePassword = user.MustChangePassword
        };
    }

    private string GenerateAccessToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("Jwt:SecretKey chưa được cấu hình.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString())
        };

        if (user.StudentId.HasValue)
        {
            claims.Add(new Claim("StudentId", user.StudentId.Value.ToString()));
        }

        if (user.CompanyId.HasValue)
        {
            claims.Add(new Claim("CompanyId", user.CompanyId.Value.ToString()));
        }

        if (user.MustChangePassword)
        {
            claims.Add(new Claim("mustChangePassword", "true"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static void Revoke(RefreshToken refreshToken)
    {
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
    }
}
