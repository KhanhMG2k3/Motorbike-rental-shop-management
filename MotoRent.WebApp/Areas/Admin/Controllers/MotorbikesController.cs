using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Maintenances;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.WebApp.Areas.Admin.Models;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.AdminOrStaff)]
public class MotorbikesController : Controller
{
    private const int PageSize = 10;

    private readonly CatalogAdminApiClient _api;
    private readonly MaintenanceApiClient _maintenanceApi;

    public MotorbikesController(CatalogAdminApiClient api, MaintenanceApiClient maintenanceApi)
    {
        _api = api;
        _maintenanceApi = maintenanceApi;
    }

    public async Task<IActionResult> Index([FromQuery] AdminMotorbikeFilter filter)
    {
        var model = new AdminMotorbikeListViewModel { Filter = filter, PageSize = PageSize };

        var search = await _api.SearchMotorbikesAsync(filter, PageSize);
        if (search.Success)
        {
            model.Items = search.Data!.Items;
            model.TotalCount = search.Data.TotalCount;
        }
        else
        {
            model.ErrorMessage = search.Message;
        }

        var branches = await _api.GetBranchesAsync();
        var brands = await _api.GetBrandsAsync();
        model.BranchOptions = (branches.Data ?? new()).Select(b => new SelectListItem(b.BranchName, b.BranchId.ToString())).ToList();
        model.BrandOptions = (brands.Data ?? new()).Select(b => new SelectListItem(b.BrandName, b.BrandId.ToString())).ToList();

        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create()
    {
        var model = new MotorbikeFormViewModel();
        model.Form.ManufactureYear = DateTime.Now.Year;
        await LoadOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] UpdateMotorbikeDto form)
    {
        var model = new MotorbikeFormViewModel { Form = form };

        if (ModelState.IsValid)
        {
            var result = await _api.CreateMotorbikeAsync(form);
            if (result.Success)
            {
                TempData["Success"] = result.Message + " Bạn có thể tải ảnh xe lên ngay bên dưới.";
                return RedirectToAction(nameof(Edit), new { id = result.Data!.MotorbikeId });
            }
            result.AddErrorsTo(ModelState, nameof(MotorbikeFormViewModel.Form));
        }

        await LoadOptionsAsync(model);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetMotorbikeAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var bike = result.Data!;
        var model = new MotorbikeFormViewModel
        {
            MotorbikeId = id,
            Current = bike,
            Form = new UpdateMotorbikeDto
            {
                LicensePlate = bike.LicensePlate,
                ModelName = bike.ModelName,
                BrandId = bike.BrandId,
                CategoryId = bike.CategoryId,
                BranchId = bike.BranchId,
                ManufactureYear = bike.ManufactureYear,
                EngineCapacity = bike.EngineCapacity,
                Color = bike.Color,
                PricePerDay = bike.PricePerDay,
                DepositAmount = bike.DepositAmount,
                Odometer = bike.Odometer,
                Description = bike.Description,
                // Giữ đường dẫn tương đối gốc (không phải URL tuyệt đối đã resolve)
                ImageUrl = bike.ImageUrl != null && Uri.TryCreate(bike.ImageUrl, UriKind.Absolute, out var abs) ? abs.AbsolutePath : bike.ImageUrl
            }
        };

        await LoadOptionsAsync(model);
        await LoadEditExtrasAsync(model, id);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Form")] UpdateMotorbikeDto form)
    {
        var model = new MotorbikeFormViewModel { MotorbikeId = id, Form = form };

        if (ModelState.IsValid)
        {
            var result = await _api.UpdateMotorbikeAsync(id, form);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
            result.AddErrorsTo(ModelState, nameof(MotorbikeFormViewModel.Form));
        }

        var current = await _api.GetMotorbikeAsync(id);
        if (current.IsNotFound) return NotFound();
        model.Current = current.Data;

        await LoadOptionsAsync(model);
        await LoadEditExtrasAsync(model, id);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(int id, IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file ảnh.";
        }
        else
        {
            var result = await _api.UploadMotorbikeImageAsync(id, imageFile);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, string status, string? returnUrl)
    {
        var result = await _api.UpdateMotorbikeStatusAsync(id, status);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Message
            : result.Errors?.Values.SelectMany(e => e).FirstOrDefault() ?? result.Message;

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _api.DeactivateMotorbikeAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOptionsAsync(MotorbikeFormViewModel model)
    {
        // Chỉ hiện hãng / loại đang hoạt động (+ giá trị đang chọn nếu đã ngừng, để không mất dữ liệu khi sửa)
        var brands = (await _api.GetBrandsAsync()).Data ?? new();
        var categories = (await _api.GetCategoriesAsync()).Data ?? new();
        var branches = (await _api.GetBranchesAsync()).Data ?? new();

        model.BrandOptions = brands
            .Where(b => b.IsActive || b.BrandId == model.Form.BrandId)
            .Select(b => new SelectListItem(b.IsActive ? b.BrandName : $"{b.BrandName} (ngừng)", b.BrandId.ToString()))
            .ToList();
        model.CategoryOptions = categories
            .Where(c => c.IsActive || c.CategoryId == model.Form.CategoryId)
            .Select(c => new SelectListItem(c.IsActive ? c.CategoryName : $"{c.CategoryName} (ngừng)", c.CategoryId.ToString()))
            .ToList();
        model.BranchOptions = branches
            .Select(b => new SelectListItem(b.BranchName, b.BranchId.ToString()))
            .ToList();
    }

    private async Task LoadEditExtrasAsync(MotorbikeFormViewModel model, int id)
    {
        var maintenances = await _maintenanceApi.GetListAsync(new MaintenanceQuery { MotorbikeId = id });
        model.Maintenances = maintenances.Data ?? new();
    }
}
