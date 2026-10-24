using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Users;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserAdminApiClient _userApi;
    private readonly BranchApiClient _branchApi;

    public UsersController(UserAdminApiClient userApi, BranchApiClient branchApi)
    {
        _userApi = userApi;
        _branchApi = branchApi;
    }

    public async Task<IActionResult> Index([FromQuery] UserFilter filter)
    {
        var usersResult = await _userApi.GetUsersAsync(filter.RoleId, filter.BranchId, filter.Keyword);
        if (!usersResult.Success) ViewData["Error"] = usersResult.Message;

        var branchesResult = await _branchApi.GetBranchesAsync(includeInactive: false);
        ViewBag.Branches = (branchesResult.Data ?? new())
            .Select(b => new SelectListItem(b.BranchName, b.BranchId.ToString(), b.BranchId == filter.BranchId))
            .ToList();

        ViewBag.Roles = new List<SelectListItem>
        {
            new("Tất cả vai trò", ""),
            new("Quản trị (Admin)", "1", filter.RoleId == 1),
            new("Nhân viên (Staff)", "2", filter.RoleId == 2),
            new("Khách hàng (Customer)", "3", filter.RoleId == 3)
        };

        ViewBag.Filter = filter;
        return View(usersResult.Data ?? new List<UserDto>());
    }

    public async Task<IActionResult> CreateStaff()
    {
        await LoadBranchesSelectListAsync();
        return View(new CreateStaffDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(CreateStaffDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadBranchesSelectListAsync();
            return View(dto);
        }

        var result = await _userApi.CreateStaffAsync(dto);
        if (!result.Success)
        {
            result.AddErrorsTo(ModelState);
            await LoadBranchesSelectListAsync();
            return View(dto);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index), new { roleId = 2 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var result = await _userApi.ToggleStatusAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadBranchesSelectListAsync()
    {
        var branchesResult = await _branchApi.GetBranchesAsync(includeInactive: false);
        ViewBag.Branches = (branchesResult.Data ?? new())
            .Select(b => new SelectListItem(b.BranchName, b.BranchId.ToString()))
            .ToList();
    }
}
