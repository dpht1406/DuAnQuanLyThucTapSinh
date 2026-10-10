namespace StudentInternshipMgmt.Domain.Enums;

public enum RequestStatus
{
    Pending = 0,
    // 1 was the removed legacy Approved status; do not reuse it because clients or data may still use the numeric value.
    Rejected = 2,
    InterviewInvited = 3,
    Accepted = 4,
    Expired = 5
}
