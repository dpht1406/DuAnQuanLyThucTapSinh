namespace StudentInternshipMgmt.Domain.Enums;

public enum RequestStatus
{
    Pending,
    // luồng cũ, gỡ ở STEP 06
    [Obsolete("luồng cũ, gỡ ở STEP 06")]
    Approved,
    Rejected,
    InterviewInvited,
    Accepted,
    Expired
}
