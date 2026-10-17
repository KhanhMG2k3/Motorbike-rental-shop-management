using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.DTOs.Brands;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
// TODO (Dev 1): [Authorize(Roles = AppRoles.Admin)] khi đã có đăng nhập
public class BrandsController : Controller
{
    private readonly CatalogAdminApiClient _api;

    public BrandsController(CatalogAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetBrandsAsync(includeInactive: true);
        if (!result.Success) ViewData["Error"] = result.Message;
        return View(result.Data ?? new List<BrandDto>());
    }

    public IActionResult Create() => View(new CreateBrandDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBrandDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _api.CreateBrandAsync(dto);
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
        var result = await _api.GetBrandAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var brand = result.Data!;
        ViewData["BrandId"] = id;
        return View(new UpdateBrandDto
        {
            BrandName = brand.BrandName,
            Country = brand.Country,
            LogoUrl = brand.LogoUrl,
            IsActive = brand.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateBrandDto dto)
    {
        ViewData["BrandId"] = id;
        if (!ModelState.IsValid) return View(dto);

        var result = await _api.UpdateBrandAsync(id, dto);
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
        var result = await _api.DeactivateBrandAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
