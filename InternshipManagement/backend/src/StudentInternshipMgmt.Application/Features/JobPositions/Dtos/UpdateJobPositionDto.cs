namespace StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

// Không có CompanyId — không cho đổi công ty sau khi đã tạo vị trí tuyển dụng.
public class UpdateJobPositionDto
{
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
}
