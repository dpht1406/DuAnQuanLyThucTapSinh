using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Application.Common;
using StudentInternshipMgmt.Application.Features.Companies;
using StudentInternshipMgmt.Application.Features.Companies.Dtos;

namespace StudentInternshipMgmt.Api.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize] 
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    // GET /api/companies — Admin + User (chỉ đọc)
    [Authorize(Roles = "Admin,User")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CompanyDto>>>> GetCompanies([FromQuery] CompanyFilterDto filter)
    {
        var result = await _companyService.GetCompaniesAsync(filter);
        return Ok(ApiResponse<PagedResult<CompanyDto>>.SuccessResponse(result));
    }

    // GET /api/companies/{id} — Admin + User (chỉ đọc), kèm danh sách JobPositions
    [Authorize(Roles = "Admin,User")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CompanyDetailDto>>> GetCompanyById(int id)
    {
        var company = await _companyService.GetCompanyByIdAsync(id);
        if (company is null)
            return NotFound(ApiResponse<CompanyDetailDto>.FailResponse("Không tìm thấy công ty."));

        return Ok(ApiResponse<CompanyDetailDto>.SuccessResponse(company));
    }

    // POST /api/companies — Admin
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> CreateCompany([FromBody] CreateCompanyDto dto)
    {
        var (success, error, data) = await _companyService.CreateCompanyAsync(dto);
        if (!success)
            return BadRequest(ApiResponse<CompanyDto>.FailResponse(error!));

        return Ok(ApiResponse<CompanyDto>.SuccessResponse(data!, "Tạo công ty thành công."));
    }

    // PUT /api/companies/{id} — Admin
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCompany(int id, [FromBody] UpdateCompanyDto dto)
    {
        var (success, error, notFound) = await _companyService.UpdateCompanyAsync(id, dto);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : BadRequest(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật công ty thành công."));
    }

    // DELETE /api/companies/{id} — Admin. Chặn xóa nếu còn Student/JobPosition liên kết.
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCompany(int id)
    {
        var (success, error, notFound) = await _companyService.DeleteCompanyAsync(id);
        if (!success)
            return notFound
                ? NotFound(ApiResponse<object>.FailResponse(error!))
                : Conflict(ApiResponse<object>.FailResponse(error!));

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa công ty thành công."));
    }
}
