using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Maintenances;
using MotoRent.WebApp.Areas.Admin.Models;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.AdminOrStaff)]
public class MaintenancesController : Controller
{
    private readonly MaintenanceApiClient _api;
    private readonly CatalogAdminApiClient _catalogApi;

    public MaintenancesController(MaintenanceApiClient api, CatalogAdminApiClient catalogApi)
    {
        _api = api;
        _catalogApi = catalogApi;
    }

    public async Task<IActionResult> Index([FromQuery] MaintenanceQuery filter)
    {
        var model = new MaintenanceListViewModel { Filter = filter };

        var result = await _api.GetListAsync(filter);
        if (result.Success) model.Items = result.Data!;
        else model.ErrorMessage = result.Message;

        var branches = await _catalogApi.GetBranchesAsync();
        model.BranchOptions = (branches.Data ?? new()).Select(b => new SelectListItem(b.BranchName, b.BranchId.ToString())).ToList();

        return View(model);
    }

    public async Task<IActionResult> Create(int? motorbikeId)
    {
        var model = new MaintenanceFormViewModel
        {
            Form = new CreateMaintenanceDto
            {
                MotorbikeId = motorbikeId ?? 0,
                StartDate = DateOnly.FromDateTime(DateTime.Today)
            }
        };
        await LoadMotorbikeOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] CreateMaintenanceDto form)
    {
        var model = new MaintenanceFormViewModel { Form = form };

        if (ModelState.IsValid)
        {
            var result = await _api.CreateAsync(form);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            result.AddErrorsTo(ModelState, nameof(MaintenanceFormViewModel.Form));
        }

        await LoadMotorbikeOptionsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var current = result.Data!;
        return View(new MaintenanceFormViewModel
        {
            MaintenanceId = id,
            Current = current,
            Form = new CreateMaintenanceDto
            {
                MotorbikeId = current.MotorbikeId,
                StartDate = current.StartDate,
                EndDate = current.EndDate,
                Description = current.Description,
                Cost = current.Cost
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Form")] CreateMaintenanceDto form)
    {
        if (ModelState.IsValid)
        {
            var result = await _api.UpdateAsync(id, form);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            result.AddErrorsTo(ModelState, nameof(MaintenanceFormViewModel.Form));
        }

        var current = await _api.GetAsync(id);
        if (current.IsNotFound) return NotFound();
        return View(new MaintenanceFormViewModel { MaintenanceId = id, Current = current.Data, Form = form });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id, string? returnUrl)
    {
        var result = await _api.StartAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Complete(int id)
    {
        var result = await _api.GetAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(new MaintenanceCompleteViewModel
        {
            Current = result.Data!,
            Form = new CompleteMaintenanceDto
            {
                EndDate = DateOnly.FromDateTime(DateTime.Today),
                Cost = result.Data!.Cost
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, [Bind(Prefix = "Form")] CompleteMaintenanceDto form)
    {
        if (ModelState.IsValid)
        {
            var result = await _api.CompleteAsync(id, form);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            result.AddErrorsTo(ModelState, nameof(MaintenanceCompleteViewModel.Form));
        }

        var current = await _api.GetAsync(id);
        if (current.IsNotFound) return NotFound();
        return View(new MaintenanceCompleteViewModel { Current = current.Data!, Form = form });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? returnUrl)
    {
        var result = await _api.CancelAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    private async Task LoadMotorbikeOptionsAsync(MaintenanceFormViewModel model)
    {
        var bikes = await _catalogApi.GetSelectableMotorbikesAsync();
        model.MotorbikeOptions = (bikes.Data?.Items ?? new())
            .Select(m => new SelectListItem($"{m.ModelName} – {m.LicensePlate} ({m.BranchName})", m.MotorbikeId.ToString()))
            .ToList();
    }
}
