using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Branches;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

[ApiController]
[Route("api/branches")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    /// <summary>Danh sách chi nhánh. Khách hàng/Public chỉ xem chi nhánh đang hoạt động, Admin có thể xem tất cả.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BranchDto>>>> GetAll([FromQuery] bool includeInactive = false)
        => Ok(ApiResponse<List<BranchDto>>.Ok(await _branchService.GetAllAsync(includeInactive)));

    /// <summary>Chi tiết chi nhánh theo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> GetById(int id)
        => Ok(ApiResponse<BranchDto>.Ok(await _branchService.GetByIdAsync(id)));

    /// <summary>Thêm chi nhánh mới (Admin).</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Create(CreateBranchDto dto)
    {
        var created = await _branchService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.BranchId },
            ApiResponse<BranchDto>.Ok(created, "Thêm chi nhánh thành công."));
    }

    /// <summary>Cập nhật chi nhánh (Admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Update(int id, UpdateBranchDto dto)
        => Ok(ApiResponse<BranchDto>.Ok(await _branchService.UpdateAsync(id, dto), "Cập nhật chi nhánh thành công."));

    /// <summary>Ngừng hoạt động chi nhánh (xoá mềm) (Admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _branchService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Đã ngừng hoạt động chi nhánh."));
    }

    /// <summary>Đổi trạng thái Hoạt động / Ngừng của chi nhánh (Admin).</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(int id)
    {
        var isActive = await _branchService.ToggleStatusAsync(id);
        var message = isActive ? "Đã kích hoạt lại chi nhánh." : "Đã ngừng hoạt động chi nhánh.";
        return Ok(ApiResponse<object>.Ok(null, message));
    }
}
