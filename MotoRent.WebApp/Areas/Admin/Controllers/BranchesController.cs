using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.DTOs.Branches;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
public class BranchesController : Controller
{
    private readonly BranchApiClient _api;

    public BranchesController(BranchApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetBranchesAsync(includeInactive: true);
        if (!result.Success) ViewData["Error"] = result.Message;
        return View(result.Data ?? new List<BranchDto>());
    }

    public IActionResult Create() => View(new CreateBranchDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBranchDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _api.CreateBranchAsync(dto);
        if (!result.Success)
        {
            result.AddErrorsTo(ModelState);
            return View(dto);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetBranchAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var branch = result.Data!;
        ViewData["BranchId"] = id;
        return View(new UpdateBranchDto
        {
            BranchName = branch.BranchName,
            Address = branch.Address,
            Phone = branch.Phone,
            OpenTime = branch.OpenTime,
            CloseTime = branch.CloseTime,
            IsActive = branch.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateBranchDto dto)
    {
        ViewData["BranchId"] = id;
        if (!ModelState.IsValid) return View(dto);

        var result = await _api.UpdateBranchAsync(id, dto);
        if (!result.Success)
        {
            result.AddErrorsTo(ModelState);
            return View(dto);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _api.DeactivateBranchAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
