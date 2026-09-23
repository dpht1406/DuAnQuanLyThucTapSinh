namespace StudentInternshipMgmt.Application.Features.Students;

public class CreateAccountsRequestDto
{
    public List<int> StudentIds { get; set; } = new();
}

// Mật khẩu plain text CHỈ tồn tại trong response này (và khi client gọi
// export ngay sau đó) — không được lưu lại plain text ở DB hay bất kỳ đâu khác.
public class CreatedAccountDto
{
    public string StudentCode { get; set; } = default!;
    public string PlainPassword { get; set; } = default!;
}

public class CreateAccountsResultDto
{
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public List<CreatedAccountDto> CreatedAccounts { get; set; } = new();
}

// Client gửi lại đúng danh sách vừa nhận từ CreateAccountsResultDto để xuất CSV.
// Xem ASSUMPTIONS.md mục "Quyết định chủ động #2" vì sao đổi GET -> POST.
public class ExportAccountsRequestDto
{
    public List<CreatedAccountDto> Accounts { get; set; } = new();
}
