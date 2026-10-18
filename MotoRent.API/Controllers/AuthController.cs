using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.API.Extensions;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.DTOs.Auth;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Đăng nhập tài khoản, trả về JWT Token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Đăng nhập thành công."));
    }

    /// <summary>Đăng ký tài khoản khách hàng mới (Customer).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Đăng ký tài khoản thành công."));
    }

    /// <summary>Xem thông tin hồ sơ của người dùng đang đăng nhập.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userId = User.GetUserId()
            ?? throw new BusinessException("Không xác định được danh tính người dùng.");
        var profile = await _authService.GetProfileAsync(userId);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>Cập nhật thông tin hồ sơ của người dùng đang đăng nhập.</summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = User.GetUserId()
            ?? throw new BusinessException("Không xác định được danh tính người dùng.");
        var profile = await _authService.UpdateProfileAsync(userId, dto);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Cập nhật hồ sơ thành công."));
    }

    /// <summary>Đổi mật khẩu người dùng đang đăng nhập.</summary>
    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordDto dto)
    {
        var userId = User.GetUserId()
            ?? throw new BusinessException("Không xác định được danh tính người dùng.");
        await _authService.ChangePasswordAsync(userId, dto);
        return Ok(ApiResponse<object>.Ok(null, "Đổi mật khẩu thành công."));
    }
}
