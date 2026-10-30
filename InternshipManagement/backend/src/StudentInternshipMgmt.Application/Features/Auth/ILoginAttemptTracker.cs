namespace StudentInternshipMgmt.Application.Features.Auth;

public interface ILoginAttemptTracker
{
    bool IsLockedOut(string username, string ipAddress, out TimeSpan retryAfter);

    bool TryBeginAttempt(string username, string ipAddress, out TimeSpan retryAfter);

    void RecordFailedAttempt(string username, string ipAddress);

    void RecordSuccessfulAttempt(string username, string ipAddress);

    void CancelAttempt(string username, string ipAddress);
}
