using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.DTOs.Categories;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Danh sách loại xe (Public).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAll([FromQuery] bool includeInactive = false)
        => Ok(ApiResponse<List<CategoryDto>>.Ok(await _categoryService.GetAllAsync(includeInactive)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(int id)
        => Ok(ApiResponse<CategoryDto>.Ok(await _categoryService.GetByIdAsync(id)));

    // TODO (Dev 1): thêm [Authorize(Roles = AppRoles.Admin)] cho POST / PUT / DELETE khi đã có JWT
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create(CreateCategoryDto dto)
    {
        var created = await _categoryService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.CategoryId },
            ApiResponse<CategoryDto>.Ok(created, "Thêm loại xe thành công."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(int id, UpdateCategoryDto dto)
        => Ok(ApiResponse<CategoryDto>.Ok(await _categoryService.UpdateAsync(id, dto), "Cập nhật loại xe thành công."));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _categoryService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Đã ngừng hoạt động loại xe."));
    }
}
