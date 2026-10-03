using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

// Không có CompanyId — không cho đổi công ty sau khi đã tạo vị trí tuyển dụng.
public class UpdateJobPositionDto
{
    [SafeString(200)]
    public string Title { get; set; } = string.Empty;
    [SafeString(100)]
    public string Department { get; set; } = string.Empty;
    [SafeString(300)]
    public string? Location { get; set; }
    public DateTime? Deadline { get; set; }
    public int Quantity { get; set; }
    [SafeString(2000, allowLineBreaks: true)]
    public string Description { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
}
