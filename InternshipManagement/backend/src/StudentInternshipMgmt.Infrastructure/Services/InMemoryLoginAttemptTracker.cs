using Microsoft.Extensions.Caching.Memory;
using StudentInternshipMgmt.Application.Features.Auth;

namespace StudentInternshipMgmt.Infrastructure.Services;

public sealed class InMemoryLoginAttemptTracker : ILoginAttemptTracker
{
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaximumAccountFailedAttempts = 5;
    private const int MaximumIpFailedAttempts = 20;

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();

    public InMemoryLoginAttemptTracker(IMemoryCache cache, TimeProvider timeProvider)
    {
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public bool IsLockedOut(string username, string ipAddress, out TimeSpan retryAfter)
    {
        lock (_sync)
        {
            var now = _timeProvider.GetUtcNow();
            var accountState = GetState(GetAccountKey(username));
            var ipState = GetState(GetIpKey(ipAddress));
            var accountRetry = GetLockoutRemaining(accountState, now);
            var ipRetry = GetLockoutRemaining(ipState, now);
            retryAfter = accountRetry >= ipRetry ? accountRetry : ipRetry;
            return retryAfter > TimeSpan.Zero;
        }
    }

    public bool TryBeginAttempt(string username, string ipAddress, out TimeSpan retryAfter)
    {
        lock (_sync)
        {
            var now = _timeProvider.GetUtcNow();
            var accountKey = GetAccountKey(username);
            var ipKey = GetIpKey(ipAddress);
            var accountState = GetState(accountKey);
            var ipState = GetState(ipKey);
            var accountRetry = GetLockoutRemaining(accountState, now);
            var ipRetry = GetLockoutRemaining(ipState, now);

            if (accountRetry > TimeSpan.Zero || ipRetry > TimeSpan.Zero)
            {
                retryAfter = accountRetry >= ipRetry ? accountRetry : ipRetry;
                return false;
            }

            if (IsAtAttemptLimit(accountState, MaximumAccountFailedAttempts) ||
                IsAtAttemptLimit(ipState, MaximumIpFailedAttempts))
            {
                retryAfter = TimeSpan.FromSeconds(1);
                return false;
            }

            SetState(accountKey, accountState with { InFlightAttempts = accountState.InFlightAttempts + 1 });
            SetState(ipKey, ipState with { InFlightAttempts = ipState.InFlightAttempts + 1 });
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    public void RecordFailedAttempt(string username, string ipAddress)
    {
        lock (_sync)
        {
            var now = _timeProvider.GetUtcNow();
            RecordFailure(GetAccountKey(username), now, MaximumAccountFailedAttempts);
            RecordFailure(GetIpKey(ipAddress), now, MaximumIpFailedAttempts);
        }
    }

    public void RecordSuccessfulAttempt(string username, string ipAddress)
    {
        lock (_sync)
        {
            var accountKey = GetAccountKey(username);
            var ipKey = GetIpKey(ipAddress);
            var accountState = GetState(accountKey);
            var ipState = GetState(ipKey);

            SetState(
                accountKey,
                accountState with
                {
                    FailedAttempts = 0,
                    InFlightAttempts = Math.Max(0, accountState.InFlightAttempts - 1),
                    LockedUntil = null
                });
            SetState(
                ipKey,
                ipState with { InFlightAttempts = Math.Max(0, ipState.InFlightAttempts - 1) });
        }
    }

    public void CancelAttempt(string username, string ipAddress)
    {
        lock (_sync)
        {
            var accountKey = GetAccountKey(username);
            var ipKey = GetIpKey(ipAddress);
            var accountState = GetState(accountKey);
            var ipState = GetState(ipKey);

            SetState(
                accountKey,
                accountState with { InFlightAttempts = Math.Max(0, accountState.InFlightAttempts - 1) });
            SetState(
                ipKey,
                ipState with { InFlightAttempts = Math.Max(0, ipState.InFlightAttempts - 1) });
        }
    }

    private AttemptState GetState(string key)
    {
        if (!_cache.TryGetValue(key, out AttemptState? state) || state is null)
        {
            return new AttemptState(0, 0, null);
        }

        var now = _timeProvider.GetUtcNow();
        if (state.LockedUntil is { } lockedUntil && lockedUntil <= now)
        {
            _cache.Remove(key);
            return new AttemptState(0, 0, null);
        }

        return state;
    }

    private void RecordFailure(string key, DateTimeOffset now, int maximumFailedAttempts)
    {
        var current = GetState(key);
        if (current.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            return;
        }

        var failedAttempts = current.FailedAttempts + 1;
        var newLockedUntil = failedAttempts >= maximumFailedAttempts
            ? now.Add(LockoutDuration)
            : (DateTimeOffset?)null;
        SetState(
            key,
            current with
            {
                FailedAttempts = failedAttempts,
                InFlightAttempts = Math.Max(0, current.InFlightAttempts - 1),
                LockedUntil = newLockedUntil
            });
    }

    private void SetState(string key, AttemptState state)
    {
        if (state.FailedAttempts == 0 && state.InFlightAttempts == 0 && state.LockedUntil is null)
        {
            _cache.Remove(key);
            return;
        }

        _cache.Set(
            key,
            state,
            new MemoryCacheEntryOptions
            {
                Size = 1,
                SlidingExpiration = state.LockedUntil is null ? AttemptWindow : null,
                AbsoluteExpiration = state.LockedUntil
            });
    }

    private static TimeSpan GetLockoutRemaining(AttemptState state, DateTimeOffset now) =>
        state.LockedUntil is { } lockedUntil && lockedUntil > now
            ? lockedUntil - now
            : TimeSpan.Zero;

    private static bool IsAtAttemptLimit(AttemptState state, int maximumFailedAttempts) =>
        state.FailedAttempts + state.InFlightAttempts >= maximumFailedAttempts;

    private static string GetAccountKey(string username) =>
        $"account:{username.Trim().ToUpperInvariant()}";

    private static string GetIpKey(string ipAddress) =>
        $"ip:{ipAddress.Trim()}";

    private sealed record AttemptState(
        int FailedAttempts,
        int InFlightAttempts,
        DateTimeOffset? LockedUntil);
}
