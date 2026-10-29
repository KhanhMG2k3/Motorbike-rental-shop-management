using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.BusinessObjects.DTOs.Reviews;
using MotoRent.WebApp.Models;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Controllers;

[Authorize(Roles = AppRoles.Customer)]
public class RentalsController : Controller
{
    private readonly RentalApiClient _rentalApi;
    private readonly MotorbikeApiClient _motorbikeApi;
    private readonly ReviewApiClient _reviewApi;
    private readonly AuthApiClient _authApi;

    public RentalsController(
        RentalApiClient rentalApi,
        MotorbikeApiClient motorbikeApi,
        ReviewApiClient reviewApi,
        AuthApiClient authApi)
    {
        _rentalApi = rentalApi;
        _motorbikeApi = motorbikeApi;
        _reviewApi = reviewApi;
        _authApi = authApi;
    }

    // GET /Rentals/MyRentals?status=Pending
    [HttpGet]
    public async Task<IActionResult> MyRentals(string? status = null)
    {
        var result = await _rentalApi.GetMyRentalsAsync();
        var model = new MyRentalsViewModel { StatusFilter = status };

        if (!result.Success)
        {
            model.ErrorMessage = result.Message;
            return View(model);
        }

        model.AllRentals = result.Data ?? new();
        model.FilteredRentals = string.IsNullOrWhiteSpace(status)
            ? model.AllRentals
            : model.AllRentals.Where(r => r.Status == status).ToList();

        return View(model);
    }

    // GET /Rentals/Create?motorbikeId=3&startDate=...&endDate=...
    [HttpGet]
    public async Task<IActionResult> Create(int motorbikeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (motorbikeId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn xe cần thuê từ danh sách xe.";
            return RedirectToAction("Index", "Motorbikes");
        }

        var bike = await _motorbikeApi.GetByIdAsync(motorbikeId);
        if (bike == null) return NotFound();

        var defaultStart = startDate ?? DateTime.Today.AddDays(1).AddHours(8);
        var defaultEnd = (endDate.HasValue && endDate.Value > defaultStart)
            ? endDate.Value
            : defaultStart.AddDays(2);

        var form = new CreateRentalDto
        {
            BranchId = bike.BranchId,
            StartDate = StripSeconds(defaultStart),
            ExpectedReturnDate = StripSeconds(defaultEnd),
            MotorbikeIds = new List<int> { bike.MotorbikeId }
        };

        var vm = await BuildCreateViewModelAsync(bike, form);
        return View(vm);
    }

    // POST /Rentals/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int primaryMotorbikeId, [Bind(Prefix = "Form")] CreateRentalDto form)
    {
        // Đảm bảo Customer không gửi CustomerId lên body (API tự lấy từ JWT)
        form.CustomerId = null;

        var bike = await _motorbikeApi.GetByIdAsync(primaryMotorbikeId);
        if (bike == null) return NotFound();

        form.BranchId = bike.BranchId;

        if (ModelState.IsValid)
        {
            var result = await _rentalApi.CreateAsync(form);
            if (result.Success && result.Data != null)
            {
                TempData["Success"] = $"Đặt thuê xe thành công! Mã đơn của bạn là {result.Data.RentalCode} (đang chờ nhân viên duyệt).";
                return RedirectToAction(nameof(MyRentals));
            }

            result.AddErrorsTo(ModelState, nameof(CustomerCreateRentalViewModel.Form));
        }

        var vm = await BuildCreateViewModelAsync(bike, form);
        return View(vm);
    }

    // GET /Rentals/Details/10
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _rentalApi.GetByIdAsync(id);
        if (result.IsNotFound) return NotFound();
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message ?? "Không thể tải thông tin đơn thuê.";
            return RedirectToAction(nameof(MyRentals));
        }

        var reviewsRes = await _reviewApi.GetByRentalAsync(id);

        return View(new CustomerRentalDetailViewModel
        {
            Rental = result.Data,
            Reviews = reviewsRes.Data ?? new(),
            CancelForm = new CancelRentalDto { RefundMethod = PaymentMethod.Cash }
        });
    }

    // POST /Rentals/Cancel/10
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, [Bind(Prefix = "CancelForm")] CancelRentalDto cancelForm)
    {
        if (!ModelState.IsValid)
        {
            var rentalRes = await _rentalApi.GetByIdAsync(id);
            if (rentalRes.Data == null) return RedirectToAction(nameof(MyRentals));
            var reviewsRes = await _reviewApi.GetByRentalAsync(id);
            return View("Details", new CustomerRentalDetailViewModel
            {
                Rental = rentalRes.Data,
                Reviews = reviewsRes.Data ?? new(),
                CancelForm = cancelForm
            });
        }

        var result = await _rentalApi.CancelAsync(id, cancelForm);
        TempData[result.Success ? "Success" : "Error"] = result.Message ?? (result.Success ? "Đã huỷ đơn thuê." : "Không thể huỷ đơn thuê.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Rentals/Review
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(CreateReviewDto dto)
    {
        var result = await _reviewApi.CreateAsync(dto);
        if (result.Success)
        {
            TempData["Success"] = result.Message ?? "Gửi đánh giá xe thành công. Cảm ơn bạn!";
        }
        else
        {
            var firstErr = result.Errors?.Values.SelectMany(e => e).FirstOrDefault();
            TempData["Error"] = firstErr ?? result.Message ?? "Không thể gửi đánh giá.";
        }

        return RedirectToAction(nameof(Details), new { id = dto.RentalId });
    }

    private async Task<CustomerCreateRentalViewModel> BuildCreateViewModelAsync(MotorbikeDto primaryBike, CreateRentalDto form)
    {
        var branchBikes = await _rentalApi.GetBranchAvailableBikesAsync(primaryBike.BranchId);
        if (branchBikes.All(b => b.MotorbikeId != primaryBike.MotorbikeId))
        {
            branchBikes.Insert(0, primaryBike);
        }

        var profileRes = await _authApi.GetProfileAsync();
        bool missingDocs = profileRes.Data != null &&
            (string.IsNullOrWhiteSpace(profileRes.Data.IdentityNumber) ||
             string.IsNullOrWhiteSpace(profileRes.Data.DriverLicenseNumber));

        return new CustomerCreateRentalViewModel
        {
            PrimaryMotorbikeId = primaryBike.MotorbikeId,
            SelectedMotorbike = primaryBike,
            BranchMotorbikes = branchBikes,
            Form = form,
            MissingDrivingDocs = missingDocs
        };
    }

    private static DateTime StripSeconds(DateTime dt)
        => new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
}