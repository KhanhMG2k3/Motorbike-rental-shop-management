using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Auth;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Controllers;

public class AuthController : Controller
{
    private readonly AuthApiClient _authApi;

    public AuthController(AuthApiClient authApi)
    {
        _authApi = authApi;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDefaultPage();

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(dto);

        var result = await _authApi.LoginAsync(dto);
        if (!result.Success || result.Data == null)
        {
            result.AddErrorsTo(ModelState);
            return View(dto);
        }

        await SignInUserAsync(result.Data);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToDefaultPage();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authApi.RegisterAsync(dto);
        if (!result.Success || result.Data == null)
        {
            result.AddErrorsTo(ModelState);
            return View(dto);
        }

        await SignInUserAsync(result.Data);
        TempData["Success"] = "Đăng ký tài khoản thành công. Chào mừng bạn đến với MotoRent!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await _authApi.GetProfileAsync();
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Index", "Home");
        }

        var profile = result.Data;
        ViewBag.UserProfile = profile;
        return View(new UpdateProfileDto
        {
            FullName = profile.FullName,
            Phone = profile.Phone,
            DateOfBirth = profile.DateOfBirth,
            Address = profile.Address,
            IdentityNumber = profile.IdentityNumber,
            DriverLicenseNumber = profile.DriverLicenseNumber
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            var pRes = await _authApi.GetProfileAsync();
            ViewBag.UserProfile = pRes.Data;
            return View(dto);
        }

        var result = await _authApi.UpdateProfileAsync(dto);
        if (!result.Success)
        {
            result.AddErrorsTo(ModelState);
            var pRes = await _authApi.GetProfileAsync();
            ViewBag.UserProfile = pRes.Data;
            return View(dto);
        }

        TempData["Success"] = "Cập nhật hồ sơ cá nhân thành công.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordDto());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _authApi.ChangePasswordAsync(dto);
        if (!result.Success)
        {
            result.AddErrorsTo(ModelState);
            return View(dto);
        }

        TempData["Success"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SignInUserAsync(AuthResponseDto auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.UserId.ToString()),
            new(ClaimTypes.Name, auth.FullName),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Role, auth.Role),
            new("access_token", auth.Token)
        };

        if (auth.BranchId.HasValue)
        {
            claims.Add(new Claim("branch_id", auth.BranchId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = auth.ExpiresAt
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);
    }

    private IActionResult RedirectToDefaultPage()
    {
        if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Staff))
            return RedirectToAction("Index", "Motorbikes", new { area = "Admin" });

        return RedirectToAction("Index", "Home");
    }
}
