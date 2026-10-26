using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.WebApp.Areas.Admin.Models;
using MotoRent.WebApp.Helpers;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.AdminOrStaff)]
public class RentalsController : Controller
{
    private const int PageSize = 10;

    private readonly RentalApiClient _rentalApi;
    private readonly CatalogAdminApiClient _catalogApi;
    private readonly BranchApiClient _branchApi;

    public RentalsController(
        RentalApiClient rentalApi,
        CatalogAdminApiClient catalogApi,
        BranchApiClient branchApi)
    {
        _rentalApi = rentalApi;
        _catalogApi = catalogApi;
        _branchApi = branchApi;
    }

    /// <summary>Danh sách đơn thuê xe (OData: lọc từ khoá, trạng thái, chi nhánh, phân trang).</summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminRentalFilter filter)
    {
        var model = new AdminRentalListViewModel
        {
            Filter = filter,
            PageSize = PageSize
        };

        var search = await _rentalApi.SearchODataAsync(
            filter.Keyword, filter.Status, filter.BranchId, filter.Page, PageSize);

        if (search.Success && search.Data != null)
        {
            model.Items = search.Data.Items;
            model.TotalCount = search.Data.TotalCount;
        }
        else
        {
            model.ErrorMessage = search.Message;
        }

        var branches = await _catalogApi.GetBranchesAsync();
        model.BranchOptions = (branches.Data ?? new())
            .Select(b => new SelectListItem(b.BranchName, b.BranchId.ToString()))
            .ToList();

        return View(model);
    }

    /// <summary>Chi tiết đơn thuê (kèm form Duyệt cọc & Huỷ đơn cho Staff).</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _rentalApi.GetByIdAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(new StaffRentalDetailViewModel
        {
            Rental = result.Data
        });
    }

    /// <summary>Staff duyệt đơn (Pending → Confirmed) và ghi nhận tiền đặt cọc.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, [Bind(Prefix = "ConfirmForm")] ConfirmRentalDto form)
    {
        if (ModelState.IsValid)
        {
            var result = await _rentalApi.ConfirmAsync(id, form);
            if (result.Success)
            {
                TempData["Success"] = result.Message ?? "Duyệt đơn và xác nhận tiền cọc thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            result.AddErrorsTo(ModelState, nameof(StaffRentalDetailViewModel.ConfirmForm));
            TempData["Error"] = result.Message;
        }

        var current = await _rentalApi.GetByIdAsync(id);
        if (current.IsNotFound) return NotFound();
        if (!current.Success || current.Data == null) return RedirectToAction(nameof(Index));

        return View("Details", new StaffRentalDetailViewModel
        {
            Rental = current.Data,
            ConfirmForm = form
        });
    }

    /// <summary>Form biên bản bàn giao xe (Confirmed → Renting).</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Pickup(int id)
    {
        var result = await _rentalApi.GetByIdAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var rental = result.Data;
        if (rental.Status != RentalStatus.Confirmed)
        {
            TempData["Error"] = $"Chỉ có thể bàn giao xe cho đơn ở trạng thái 'Đã xác nhận'. Trạng thái hiện tại: {DisplayHelper.RentalStatusText(rental.Status)}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = await BuildPickupViewModelAsync(rental, null);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pickup(int id, [Bind(Prefix = "Form")] PickupRentalDto form)
    {
        if (ModelState.IsValid)
        {
            var result = await _rentalApi.PickupAsync(id, form);
            if (result.Success)
            {
                TempData["Success"] = result.Message ?? "Bàn giao xe thành công! Đơn đã chuyển sang trạng thái Đang thuê.";
                return RedirectToAction(nameof(Details), new { id });
            }
            result.AddErrorsTo(ModelState, nameof(StaffPickupViewModel.Form));
        }

        var current = await _rentalApi.GetByIdAsync(id);
        if (current.IsNotFound) return NotFound();
        if (!current.Success || current.Data == null) return RedirectToAction(nameof(Index));

        var vm = await BuildPickupViewModelAsync(current.Data, form);
        return View(vm);
    }

    /// <summary>Form nhận trả xe & quyết toán chi phí (Renting → Completed).</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Return(int id)
    {
        var result = await _rentalApi.GetByIdAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var rental = result.Data;
        if (rental.Status != RentalStatus.Renting)
        {
            TempData["Error"] = $"Chỉ có thể nhận trả xe cho đơn ở trạng thái 'Đang thuê'. Trạng thái hiện tại: {DisplayHelper.RentalStatusText(rental.Status)}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Mặc định giờ hiện tại; nếu đơn test có StartDate ở tương lai thì lấy ExpectedReturnDate để không vi phạm ActualReturnDate >= StartDate
        var now = StripSeconds(DateTime.Now);
        var defaultReturn = now >= rental.StartDate ? now : StripSeconds(rental.ExpectedReturnDate);

        var vm = new StaffReturnViewModel
        {
            Rental = rental,
            Form = new ReturnRentalDto
            {
                ActualReturnDate = defaultReturn,
                DamageFee = 0,
                DiscountAmount = 0,
                PaymentMethod = PaymentMethod.Cash,
                Items = rental.Details.Select(d => new ReturnItemDto
                {
                    MotorbikeId = d.MotorbikeId,
                    ReturnOdometer = d.ReturnOdometer ?? (d.PickupOdometer ?? 0),
                    ReturnCondition = d.ReturnCondition ?? "Xe nguyên vẹn, hoạt động bình thường",
                    IsDamaged = false
                }).ToList()
            }
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id, [Bind(Prefix = "Form")] ReturnRentalDto form)
    {
        if (ModelState.IsValid)
        {
            var result = await _rentalApi.ReturnAsync(id, form);
            if (result.Success)
            {
                TempData["Success"] = result.Message ?? "Nhận trả xe và quyết toán thành công! Bạn có thể in hoá đơn ngay bên dưới.";
                return RedirectToAction(nameof(Invoice), new { id });
            }
            result.AddErrorsTo(ModelState, nameof(StaffReturnViewModel.Form));
        }

        var current = await _rentalApi.GetByIdAsync(id);
        if (current.IsNotFound) return NotFound();
        if (!current.Success || current.Data == null) return RedirectToAction(nameof(Index));

        return View(new StaffReturnViewModel
        {
            Rental = current.Data,
            Form = AlignReturnItems(current.Data, form)
        });
    }

    /// <summary>Hoá đơn quyết toán đơn thuê (cho phép xem & in khi đơn đã Completed).</summary>
    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var result = await _rentalApi.GetByIdAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var rental = result.Data;
        if (rental.Status != RentalStatus.Completed)
        {
            TempData["Error"] = "Hoá đơn quyết toán chỉ được xuất sau khi đơn thuê đã hoàn thành trả xe.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var branchRes = await _branchApi.GetBranchAsync(rental.BranchId);

        return View(new StaffInvoiceViewModel
        {
            Rental = rental,
            Branch = branchRes.Data
        });
    }

    /// <summary>Staff huỷ đơn thuê (Pending hoặc Confirmed, tự hoàn cọc nếu đã thu cọc).</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, [Bind(Prefix = "CancelForm")] CancelRentalDto form)
    {
        if (ModelState.IsValid)
        {
            var result = await _rentalApi.CancelAsync(id, form);
            if (result.Success)
            {
                TempData["Success"] = result.Message ?? "Đã huỷ đơn thuê thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            result.AddErrorsTo(ModelState, nameof(StaffRentalDetailViewModel.CancelForm));
            TempData["Error"] = result.Message;
        }

        var current = await _rentalApi.GetByIdAsync(id);
        if (current.IsNotFound) return NotFound();
        if (!current.Success || current.Data == null) return RedirectToAction(nameof(Index));

        return View("Details", new StaffRentalDetailViewModel
        {
            Rental = current.Data,
            CancelForm = form
        });
    }

    // ===== Helpers =====

    private async Task<StaffPickupViewModel> BuildPickupViewModelAsync(RentalDto rental, PickupRentalDto? postedForm)
    {
        var currentOdometers = new Dictionary<int, int>();
        var items = new List<PickupItemDto>();

        foreach (var d in rental.Details)
        {
            var bikeRes = await _catalogApi.GetMotorbikeAsync(d.MotorbikeId);
            int currentKm = bikeRes.Data?.Odometer ?? 0;
            currentOdometers[d.MotorbikeId] = currentKm;

            var existing = postedForm?.Items.FirstOrDefault(x => x.MotorbikeId == d.MotorbikeId);
            items.Add(existing ?? new PickupItemDto
            {
                MotorbikeId = d.MotorbikeId,
                PickupOdometer = d.PickupOdometer ?? currentKm,
                PickupCondition = d.PickupCondition ?? "Xe hoạt động tốt, đủ gương, mũ bảo hiểm và giấy tờ"
            });
        }

        return new StaffPickupViewModel
        {
            Rental = rental,
            CurrentOdometers = currentOdometers,
            Form = new PickupRentalDto { Items = items }
        };
    }

    private static ReturnRentalDto AlignReturnItems(RentalDto rental, ReturnRentalDto form)
    {
        form.Items = rental.Details.Select(d =>
            form.Items.FirstOrDefault(x => x.MotorbikeId == d.MotorbikeId) ?? new ReturnItemDto
            {
                MotorbikeId = d.MotorbikeId,
                ReturnOdometer = d.PickupOdometer ?? 0
            }).ToList();
        return form;
    }

    private static DateTime StripSeconds(DateTime dt)
        => new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
}