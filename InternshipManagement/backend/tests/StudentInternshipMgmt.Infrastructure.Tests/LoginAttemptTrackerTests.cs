using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentInternshipMgmt.Api.Controllers;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Auth;
using StudentInternshipMgmt.Application.Features.Auth.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class LoginAttemptTrackerTests
{
    [Fact]
    public void Login_and_refresh_use_the_auth_rate_limit_policy()
    {
        typeof(AuthController).GetMethod(nameof(AuthController.Login))!
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>()
            .Single()
            .PolicyName.Should().Be("auth");
        typeof(AuthController).GetMethod(nameof(AuthController.Refresh))!
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>()
            .Single()
            .PolicyName.Should().Be("auth");
    }

    [Fact]
    public async Task Fifth_failure_locks_next_login_until_15_minutes_pass()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var clock = new ManualTimeProvider();
        var tracker = new InMemoryLoginAttemptTracker(cache, clock);
        var authService = new TestAuthService();
        var controller = CreateController(authService, tracker, "192.0.2.10");

        for (var index = 0; index < 4; index++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                controller.Login(new LoginRequestDto { Username = "user", Password = "wrong" }, default));
        }
        tracker.IsLockedOut("user", "192.0.2.10", out _).Should().BeFalse();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            controller.Login(new LoginRequestDto { Username = "user", Password = "wrong" }, default));

        var blockedResponse = await controller.Login(
            new LoginRequestDto { Username = "user", Password = "CorrectPass2026" },
            default);
        var blockedResult = blockedResponse.Result.Should().BeOfType<ObjectResult>().Subject;
        blockedResult.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        blockedResult.Value.Should().BeOfType<ApiResponse<LoginResponseDto>>()
            .Which.Message.Should().Contain("15 phút");
        int.Parse(controller.Response.Headers.RetryAfter!).Should().Be(900);
        authService.LoginCalls.Should().Be(5);

        clock.Advance(TimeSpan.FromMinutes(15));
        var unlockedResponse = await controller.Login(
            new LoginRequestDto { Username = "user", Password = "CorrectPass2026" },
            default);
        unlockedResponse.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Successful_login_clears_account_failures_across_ips()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var tracker = new InMemoryLoginAttemptTracker(cache, new ManualTimeProvider());
        var authService = new TestAuthService();
        var firstIpController = CreateController(authService, tracker, "192.0.2.10");
        var secondIpController = CreateController(authService, tracker, "192.0.2.11");

        for (var index = 0; index < 4; index++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                firstIpController.Login(new LoginRequestDto { Username = "user", Password = "wrong" }, default));
        }
        (await secondIpController.Login(
            new LoginRequestDto { Username = "user", Password = "CorrectPass2026" },
            default)).Result.Should().BeOfType<OkObjectResult>();

        tracker.IsLockedOut("USER", "192.0.2.10", out _).Should().BeFalse();
        tracker.IsLockedOut("user", "192.0.2.11", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Failed_attempts_are_shared_across_ips_and_trim_username()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var tracker = new InMemoryLoginAttemptTracker(cache, new ManualTimeProvider());
        var authService = new TestAuthService();
        var firstIpController = CreateController(authService, tracker, "192.0.2.10");
        var secondIpController = CreateController(authService, tracker, "192.0.2.11");
        var thirdIpController = CreateController(authService, tracker, "192.0.2.12");

        for (var index = 0; index < 4; index++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                firstIpController.Login(new LoginRequestDto { Username = "user", Password = "wrong" }, default));
        }

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            secondIpController.Login(new LoginRequestDto { Username = " user ", Password = "wrong" }, default));

        var blockedResponse = await thirdIpController.Login(
            new LoginRequestDto { Username = "USER", Password = "CorrectPass2026" },
            default);
        blockedResponse.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        authService.LoginCalls.Should().Be(5);
    }

    [Fact]
    public async Task Concurrent_login_attempts_reserve_slots_before_authentication()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var tracker = new InMemoryLoginAttemptTracker(cache, new ManualTimeProvider());
        var allCallsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCalls = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var authService = new TestAuthService(allCallsStarted, releaseCalls, targetCalls: 5);
        var loginTasks = Enumerable.Range(0, 20)
            .Select(async _ =>
            {
                var controller = CreateController(authService, tracker, "192.0.2.20");
                try
                {
                    var result = await controller.Login(
                        new LoginRequestDto { Username = "user", Password = "wrong" },
                        default);
                    return result.Result;
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }
            })
            .ToArray();

        await allCallsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        releaseCalls.SetResult();
        var results = await Task.WhenAll(loginTasks);

        authService.LoginCalls.Should().Be(5);
        results.OfType<ObjectResult>()
            .Count(result => result.StatusCode == StatusCodes.Status429TooManyRequests)
            .Should().Be(15);
        tracker.IsLockedOut("user", "192.0.2.20", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Unknown_username_is_counted_and_failed_login_log_omits_username()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var tracker = new InMemoryLoginAttemptTracker(cache, new ManualTimeProvider());
        var authService = new TestAuthService();
        var controller = CreateController(authService, tracker, "192.0.2.12");

        for (var index = 0; index < 5; index++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                controller.Login(new LoginRequestDto { Username = "missing-account", Password = "wrong" }, default));
        }
        tracker.IsLockedOut("missing-account", "192.0.2.12", out _).Should().BeTrue();

        await using var dbContext = CreateDbContext();
        var logger = new CapturingLogger<AuthService>();
        var auth = new AuthService(dbContext, Options.Create(new JwtSettings
        {
            Issuer = "login-attempt-tests",
            Audience = "login-attempt-tests",
            SecretKey = "login-attempt-tests-secret-key-at-least-32-characters",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        }), logger);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.LoginAsync(new LoginRequestDto { Username = "missing-account", Password = "wrong" }));

        logger.Messages.Should().ContainSingle();
        logger.Messages[0].Should().MatchRegex(@"Login failed for username hash [A-F0-9]{8}")
            .And.NotContain("missing-account");
    }

    private static AuthController CreateController(
        IAuthService authService,
        ILoginAttemptTracker tracker,
        string ipAddress)
    {
        var controller = new AuthController(authService, tracker)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        return controller;
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class TestAuthService : IAuthService
    {
        private readonly TaskCompletionSource? _allCallsStarted;
        private readonly TaskCompletionSource? _releaseCalls;
        private readonly int _targetCalls;
        private int _loginCalls;

        public TestAuthService(
            TaskCompletionSource? allCallsStarted = null,
            TaskCompletionSource? releaseCalls = null,
            int targetCalls = 0)
        {
            _allCallsStarted = allCallsStarted;
            _releaseCalls = releaseCalls;
            _targetCalls = targetCalls;
        }

        public int LoginCalls => Volatile.Read(ref _loginCalls);

        public async Task<LoginResponseDto> LoginAsync(
            LoginRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var callCount = Interlocked.Increment(ref _loginCalls);
            if (callCount == _targetCalls)
            {
                _allCallsStarted?.TrySetResult();
            }

            if (_releaseCalls is not null)
            {
                await _releaseCalls.Task.WaitAsync(cancellationToken);
            }

            if (request.Password != "CorrectPass2026")
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            return new LoginResponseDto
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            };
        }

        public Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<MeResponseDto> GetMeAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
