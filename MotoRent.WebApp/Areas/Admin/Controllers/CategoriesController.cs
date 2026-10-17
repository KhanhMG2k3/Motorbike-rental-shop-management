using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.DTOs.Categories;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
// TODO (Dev 1): [Authorize(Roles = AppRoles.Admin)] khi đã có đăng nhập
public class CategoriesController : Controller
{
    private readonly CatalogAdminApiClient _api;

    public CategoriesController(CatalogAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetCategoriesAsync(includeInactive: true);
        if (!result.Success) ViewData["Error"] = result.Message;
        return View(result.Data ?? new List<CategoryDto>());
    }

    public IActionResult Create() => View(new CreateCategoryDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _api.CreateCategoryAsync(dto);
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
        var result = await _api.GetCategoryAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var category = result.Data!;
        ViewData["CategoryId"] = id;
        return View(new UpdateCategoryDto
        {
            CategoryName = category.CategoryName,
            Description = category.Description,
            IsActive = category.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateCategoryDto dto)
    {
        ViewData["CategoryId"] = id;
        if (!ModelState.IsValid) return View(dto);

        var result = await _api.UpdateCategoryAsync(id, dto);
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
        var result = await _api.DeactivateCategoryAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
