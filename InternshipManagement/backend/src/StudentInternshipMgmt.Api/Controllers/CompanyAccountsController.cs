using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Companies.Accounts;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:int}/account")]
[Authorize(Roles = "Admin")]
public class CompanyAccountsController : ControllerBase
{
    private readonly ICompanyAccountService _companyAccountService;

    public CompanyAccountsController(ICompanyAccountService companyAccountService)
    {
        _companyAccountService = companyAccountService;
    }

    [HttpGet("")]
    public async Task<ActionResult<ApiResponse<CompanyAccountDto>>> GetAccount(int companyId)
    {
        var account = await _companyAccountService.GetAccountAsync(companyId);
        if (account is null)
            return NotFound(ApiResponse<CompanyAccountDto>.FailResponse("Không tìm thấy công ty."));

        return Ok(ApiResponse<CompanyAccountDto>.SuccessResponse(account));
    }

    [HttpPost("")]
    public async Task<ActionResult<ApiResponse<CompanyAccountCredentialsDto>>> CreateAccount(int companyId)
    {
        Response.Headers["Cache-Control"] = "no-store";
        var adminUserId = GetCurrentAdminId();
        if (adminUserId is null)
            return Unauthorized(ApiResponse<CompanyAccountCredentialsDto>.FailResponse(
                "Không xác định được người dùng hiện tại."));

        var (success, error, notFound, data) =
            await _companyAccountService.CreateAccountAsync(companyId, adminUserId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<CompanyAccountCredentialsDto>.FailResponse(error!))
                : BadRequest(ApiResponse<CompanyAccountCredentialsDto>.FailResponse(error!));

        return Ok(ApiResponse<CompanyAccountCredentialsDto>.SuccessResponse(
            data!, "Tạo tài khoản doanh nghiệp thành công."));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<CompanyAccountCredentialsDto>>> ResetPassword(int companyId)
    {
        Response.Headers["Cache-Control"] = "no-store";
        var adminUserId = GetCurrentAdminId();
        if (adminUserId is null)
            return Unauthorized(ApiResponse<CompanyAccountCredentialsDto>.FailResponse(
                "Không xác định được người dùng hiện tại."));

        var (success, error, notFound, data) =
            await _companyAccountService.ResetPasswordAsync(companyId, adminUserId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<CompanyAccountCredentialsDto>.FailResponse(error!))
                : BadRequest(ApiResponse<CompanyAccountCredentialsDto>.FailResponse(error!));

        return Ok(ApiResponse<CompanyAccountCredentialsDto>.SuccessResponse(data!));
    }

    [HttpPost("deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(int companyId)
    {
        var adminUserId = GetCurrentAdminId();
        if (adminUserId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error, notFound) =
            await _companyAccountService.DeactivateAsync(companyId, adminUserId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Vô hiệu hóa tài khoản doanh nghiệp thành công."));
    }

    [HttpPost("activate")]
    public async Task<ActionResult<ApiResponse<object>>> Activate(int companyId)
    {
        var adminUserId = GetCurrentAdminId();
        if (adminUserId is null)
            return Unauthorized(ApiResponse<object>.FailResponse("Không xác định được người dùng hiện tại."));

        var (success, error, notFound) =
            await _companyAccountService.ActivateAsync(companyId, adminUserId.Value);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Kích hoạt tài khoản doanh nghiệp thành công."));
    }

    private int? GetCurrentAdminId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}