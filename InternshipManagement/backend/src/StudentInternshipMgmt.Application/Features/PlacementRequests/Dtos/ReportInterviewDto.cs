using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.PlacementRequests.Dtos;

/// <summary>Thông tin sinh viên báo đã tham gia phỏng vấn.</summary>
public class ReportInterviewDto
{
    /// <summary>Ghi chú tùy chọn của sinh viên về buổi phỏng vấn.</summary>
    [SafeString(500, allowLineBreaks: true)]
    public string? Note { get; set; }
}
