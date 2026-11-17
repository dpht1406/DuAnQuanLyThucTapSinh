using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Companies.Accounts;
using StudentInternshipMgmt.Application.Features.Companies.Dtos;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/companies/accounts")]
[Authorize(Roles = "Admin")]
public class CompanyAccountsBulkController : ControllerBase
{
    private const int MaxBulkLimit = 500;
    private readonly ICompanyAccountService _companyAccountService;

    public CompanyAccountsBulkController(ICompanyAccountService companyAccountService)
    {
        _companyAccountService = companyAccountService;
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<ApiResponse<CreateCompanyAccountsResultDto>>> CreateAccountsBulk(
        [FromBody] CreateCompanyAccountsRequestDto dto)
    {
        if (Response != null)
        {
            Response.Headers["Cache-Control"] = "no-store";
        }

        if (dto?.CompanyIds is null || dto.CompanyIds.Count == 0)
        {
            return BadRequest(ApiResponse<CreateCompanyAccountsResultDto>.FailResponse(
                "Danh sách mã doanh nghiệp không được để trống."));
        }

        if (dto.CompanyIds.Count > MaxBulkLimit)
        {
            return BadRequest(ApiResponse<CreateCompanyAccountsResultDto>.FailResponse(
                $"Số lượng doanh nghiệp vượt quá giới hạn cho phép (tối đa {MaxBulkLimit})."));
        }

        var adminUserId = GetCurrentAdminId();
        if (adminUserId is null)
        {
            return Unauthorized(ApiResponse<CreateCompanyAccountsResultDto>.FailResponse(
                "Không xác định được người dùng hiện tại."));
        }

        var result = await _companyAccountService.CreateAccountsBulkAsync(dto.CompanyIds, adminUserId.Value);
        return Ok(ApiResponse<CreateCompanyAccountsResultDto>.SuccessResponse(
            result, "Xử lý tạo tài khoản doanh nghiệp hàng loạt hoàn tất."));
    }

    [HttpPost("bulk-by-filter")]
    public async Task<ActionResult<ApiResponse<CreateCompanyAccountsResultDto>>> CreateAccountsBulkByFilter(
        [FromBody] CompanyFilterDto filter)
    {
        if (Response != null)
        {
            Response.Headers["Cache-Control"] = "no-store";
        }

        var adminUserId = GetCurrentAdminId();
        if (adminUserId is null)
        {
            return Unauthorized(ApiResponse<CreateCompanyAccountsResultDto>.FailResponse(
                "Không xác định được người dùng hiện tại."));
        }

        var (success, error, data) = await _companyAccountService.CreateAccountsBulkByFilterAsync(
            filter ?? new CompanyFilterDto(),
            adminUserId.Value);

        if (!success)
        {
            return BadRequest(ApiResponse<CreateCompanyAccountsResultDto>.FailResponse(error!));
        }

        return Ok(ApiResponse<CreateCompanyAccountsResultDto>.SuccessResponse(
            data!, "Xử lý tạo tài khoản doanh nghiệp hàng loạt theo bộ lọc hoàn tất."));
    }

    [HttpPost("export")]
    public IActionResult ExportAccounts([FromBody] ExportCompanyAccountsRequestDto dto)
    {
        if (Response != null)
        {
            Response.Headers["Cache-Control"] = "no-store";
        }

        if (dto?.Accounts is null || dto.Accounts.Count == 0)
        {
            return BadRequest(ApiResponse<object>.FailResponse("Danh sách tài khoản trống."));
        }

        var sb = new StringBuilder();
        sb.AppendLine("Username,TemporaryPassword,CompanyName,ContactEmail");
        foreach (var acc in dto.Accounts)
        {
            var u = SpreadsheetSafety.EscapeCsvField(acc.Username);
            var p = SpreadsheetSafety.EscapeCsvField(acc.TemporaryPassword);
            var c = SpreadsheetSafety.EscapeCsvField(acc.CompanyName);
            var e = SpreadsheetSafety.EscapeCsvField(acc.ContactEmail);
            sb.AppendLine($"{u},{p},{c},{e}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"company_accounts_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private int? GetCurrentAdminId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}
