using Microsoft.AspNetCore.Mvc;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Controllers;

/// <summary>Trang công khai: danh sách chi nhánh + bản đồ vị trí (Guest/Customer).</summary>
public class BranchesController : Controller
{
    private readonly BranchApiClient _api;

    public BranchesController(BranchApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetBranchesAsync(includeInactive: false);
        if (!result.Success) ViewData["Error"] = result.Message;
        return View(result.Data ?? new List<BusinessObjects.DTOs.Branches.BranchDto>());
    }
}
