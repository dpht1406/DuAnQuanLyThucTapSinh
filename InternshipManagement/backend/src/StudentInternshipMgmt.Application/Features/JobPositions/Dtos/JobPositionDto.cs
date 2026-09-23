namespace StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

// Đây là JobPositionDto "đầy đủ" cho chính feature JobPositions (Giai đoạn 4b) —
// khác với StudentInternshipMgmt.Application.Features.Companies.Dtos.JobPositionDto,
// vốn chỉ là bản tối giản (Id, Title, Quantity, IsOpen) dùng để lồng trong
// CompanyDetailDto. Không sửa file đó.
public class JobPositionDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
}
