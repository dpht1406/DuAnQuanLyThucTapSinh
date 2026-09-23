namespace StudentInternshipMgmt.Application.Features.JobPositions.Dtos;

// Dùng cho Admin. Giống JobPositionDto, thêm AcceptedCount = số Student
// (đã qua global query filter !IsDeleted của StudentConfiguration) có
// JobPositionId = vị trí này và Status thuộc {Accepted, Interning, Completed}.
public class JobPositionAdminDto : JobPositionDto
{
    public int AcceptedCount { get; set; }
}
