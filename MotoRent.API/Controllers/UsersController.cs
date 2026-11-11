using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.API.Extensions;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Users;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Danh sách người dùng (Admin). Có thể lọc theo vai trò (roleId), chi nhánh (branchId), hoặc từ khoá (keyword).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAll([FromQuery] int? roleId, [FromQuery] int? branchId, [FromQuery] string? keyword)
    {
        var users = await _userService.GetUsersAsync(roleId, branchId, keyword);
        return Ok(ApiResponse<List<UserDto>>.Ok(users));
    }

    /// <summary>Chi tiết người dùng theo Id (Admin).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>Tạo tài khoản nhân viên mới gán vào chi nhánh (Admin).</summary>
    [HttpPost("staff")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateStaff(CreateStaffDto dto)
    {
        var created = await _userService.CreateStaffAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.UserId },
            ApiResponse<UserDto>.Ok(created, "Tạo tài khoản nhân viên thành công."));
    }

    /// <summary>Khoá hoặc mở khoá tài khoản (Admin). Không thể tự khoá chính mình.</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(int id)
    {
        var currentAdminId = User.GetUserId()
            ?? throw new BusinessException("Không xác định được danh tính Admin.");

        await _userService.ToggleStatusAsync(id, currentAdminId);
        return Ok(ApiResponse<object>.Ok(null, "Cập nhật trạng thái tài khoản thành công."));
    }
}
