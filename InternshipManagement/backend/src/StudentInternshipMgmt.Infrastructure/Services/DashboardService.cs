using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudentInternshipMgmt.Application.Features.Dashboard;
using StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public DashboardService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var total = await _db.Students.CountAsync();
        var withCompany = await _db.Students.CountAsync(s => s.CompanyId != null);
        var completed = await _db.Students.CountAsync(s => s.Status == StudentStatus.Completed);

        return new DashboardSummaryDto
        {
            TotalStudents = total,
            WithCompanyCount = withCompany,
            WithoutCompanyCount = total - withCompany,
            CompletedCount = completed,
            CompletionRate = total == 0 ? 0 : Math.Round((double)completed / total * 100, 2)
        };
    }

    public async Task<List<NeedAssignmentDto>> GetNeedAssignmentListAsync()
    {
        var reminderDays = _configuration.GetValue<int?>("PlacementReminderDays") ?? 5;
        var cutoff = DateTime.UtcNow.AddDays(-reminderDays);

        var students = await _db.Students
            .Include(s => s.User)
            .Where(s => s.Status == StudentStatus.NoCompany
                && s.User != null
                && s.User.CreatedAt <= cutoff
                && !_db.PlacementRequests.Any(pr => pr.StudentId == s.Id && pr.Status == RequestStatus.Pending))
            .OrderBy(s => s.User!.CreatedAt)
            .ToListAsync();

        return students.Select(s => new NeedAssignmentDto
        {
            StudentId = s.Id,
            StudentCode = s.StudentCode,
            FullName = s.FullName,
            Email = s.Email,
            PhoneNumber = s.PhoneNumber,
            AccountCreatedAt = s.User!.CreatedAt,
            DaysSinceCreated = (int)(DateTime.UtcNow - s.User!.CreatedAt).TotalDays
        }).ToList();
    }

    public async Task<List<PlacementRequestDto>> GetOverdueRequestsAsync()
    {
        var overdueAlertDays = _configuration.GetValue<int?>("OverdueRequestAlertDays") ?? 5;
        if (overdueAlertDays <= 0)
        {
            overdueAlertDays = 5;
        }

        var cutoff = DateTime.UtcNow.AddDays(-overdueAlertDays);
        return await _db.PlacementRequests
            .AsNoTracking()
            .Where(request => request.Status == RequestStatus.Pending
                && request.CreatedAt <= cutoff)
            .OrderBy(request => request.CreatedAt)
            .Select(request => new PlacementRequestDto
            {
                Id = request.Id,
                StudentId = request.StudentId,
                StudentCode = request.Student.StudentCode,
                StudentFullName = request.Student.FullName,
                CompanyId = request.CompanyId,
                CompanyName = request.Company.Name,
                JobPositionId = request.JobPositionId,
                JobPositionTitle = request.JobPosition != null ? request.JobPosition.Title : null,
                ApplicantFullName = request.ApplicantFullName,
                ApplicantEmail = request.ApplicantEmail,
                ApplicantPhone = request.ApplicantPhone,
                ApplicantSchool = request.ApplicantSchool,
                ApplicantMajor = request.ApplicantMajor,
                CvUrl = request.CvUrl,
                CoverLetter = request.CoverLetter,
                Status = request.Status,
                Note = request.Note,
                RejectReason = request.RejectReason,
                CreatedAt = request.CreatedAt,
                DueAt = request.DueAt,
                InterviewAt = request.InterviewAt,
                InterviewLocation = request.InterviewLocation,
                InterviewNote = request.InterviewNote,
                ResultNote = request.ResultNote,
                StudentReportedInterviewAt = request.StudentReportedInterviewAt,
                StudentInterviewNote = request.StudentInterviewNote,
                ResultActorType = request.ResultActorType,
                ReviewedBy = request.ReviewedBy,
                ReviewedByUsername = request.ReviewedByUser != null ? request.ReviewedByUser.Username : null,
                ReviewedAt = request.ReviewedAt
            })
            .ToListAsync();
    }

    public async Task<List<PlacementRequestDto>> GetPendingInterviewResultsAsync()
    {
        var overdueDays = _configuration.GetValue<int?>("InterviewResultOverdueDays") ?? 3;
        if (overdueDays <= 0)
        {
            overdueDays = 3;
        }

        var cutoff = DateTime.UtcNow.AddDays(-overdueDays);
        return await _db.PlacementRequests
            .AsNoTracking()
            .Where(request => request.Status == RequestStatus.InterviewInvited
                && request.InterviewAt.HasValue
                && request.InterviewAt <= cutoff)
            .OrderBy(request => request.InterviewAt)
            .Select(request => new PlacementRequestDto
            {
                Id = request.Id,
                StudentId = request.StudentId,
                StudentCode = request.Student.StudentCode,
                StudentFullName = request.Student.FullName,
                CompanyId = request.CompanyId,
                CompanyName = request.Company.Name,
                JobPositionId = request.JobPositionId,
                JobPositionTitle = request.JobPosition != null ? request.JobPosition.Title : null,
                ApplicantFullName = request.ApplicantFullName,
                ApplicantEmail = request.ApplicantEmail,
                ApplicantPhone = request.ApplicantPhone,
                ApplicantSchool = request.ApplicantSchool,
                ApplicantMajor = request.ApplicantMajor,
                CvUrl = request.CvUrl,
                CoverLetter = request.CoverLetter,
                Status = request.Status,
                Note = request.Note,
                RejectReason = request.RejectReason,
                CreatedAt = request.CreatedAt,
                DueAt = request.DueAt,
                InterviewAt = request.InterviewAt,
                InterviewLocation = request.InterviewLocation,
                InterviewNote = request.InterviewNote,
                ResultNote = request.ResultNote,
                StudentReportedInterviewAt = request.StudentReportedInterviewAt,
                StudentInterviewNote = request.StudentInterviewNote,
                ResultActorType = request.ResultActorType,
                ReviewedBy = request.ReviewedBy,
                ReviewedByUsername = request.ReviewedByUser != null ? request.ReviewedByUser.Username : null,
                ReviewedAt = request.ReviewedAt
            })
            .ToListAsync();
    }

    public async Task<List<RecentRejectionDto>> GetRecentRejectionsAsync()
    {
        var recentDays = Math.Max(1, _configuration.GetValue<int?>("RecentRejectionDays") ?? 14);
        var cutoff = DateTime.UtcNow.AddDays(-recentDays);
        var studentsWithoutPendingRequest = _db.Students
            .Where(student => student.Status == StudentStatus.NoCompany
                && !_db.PlacementRequests.Any(request =>
                    request.StudentId == student.Id && request.Status == RequestStatus.Pending));

        var latestHistories = _db.StatusHistories
            .Where(history => !_db.StatusHistories.Any(newer =>
                newer.StudentId == history.StudentId
                && (newer.ChangedAt > history.ChangedAt
                    || (newer.ChangedAt == history.ChangedAt && newer.Id > history.Id))));

        return await latestHistories
            .Where(history => history.ToStatus == StudentStatus.NoCompany
                && history.FromStatus != StudentStatus.NoCompany
                && history.ChangedAt >= cutoff
                && history.ChangedByUser.Role == UserRole.User)
            .Join(studentsWithoutPendingRequest,
                history => history.StudentId,
                student => student.Id,
                (history, student) => new { history, student })
            .OrderByDescending(item => item.history.ChangedAt)
            .ThenByDescending(item => item.history.Id)
            .Select(item => new RecentRejectionDto
            {
                StudentId = item.student.Id,
                StudentCode = item.student.StudentCode,
                FullName = item.student.FullName,
                RejectedCompanyName = item.history.Company != null ? item.history.Company.Name : string.Empty,
                Reason = item.history.Note,
                RejectedAt = item.history.ChangedAt
            })
            .ToListAsync();
    }
}
