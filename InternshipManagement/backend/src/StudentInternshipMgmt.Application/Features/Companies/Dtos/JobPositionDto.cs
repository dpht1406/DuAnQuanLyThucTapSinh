namespace StudentInternshipMgmt.Application.Features.Companies.Dtos;

// Bản tối giản, chỉ phục vụ hiển thị lồng trong CompanyDetailDto.
// Giai đoạn 4b (feature JobPositions) sẽ có DTO đầy đủ hơn (Description, "đã có X/Y"...).
public class JobPositionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsOpen { get; set; }
}
