using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Brands;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    /// <summary>Danh sách hãng xe (Public). Admin truyền includeInactive=true để xem cả hãng đã ngừng.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BrandDto>>>> GetAll([FromQuery] bool includeInactive = false)
        => Ok(ApiResponse<List<BrandDto>>.Ok(await _brandService.GetAllAsync(includeInactive)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<BrandDto>>> GetById(int id)
        => Ok(ApiResponse<BrandDto>.Ok(await _brandService.GetByIdAsync(id)));

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<BrandDto>>> Create(CreateBrandDto dto)
    {
        var created = await _brandService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.BrandId },
            ApiResponse<BrandDto>.Ok(created, "Thêm hãng xe thành công."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<BrandDto>>> Update(int id, UpdateBrandDto dto)
        => Ok(ApiResponse<BrandDto>.Ok(await _brandService.UpdateAsync(id, dto), "Cập nhật hãng xe thành công."));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _brandService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Đã ngừng hoạt động hãng xe."));
    }
}
