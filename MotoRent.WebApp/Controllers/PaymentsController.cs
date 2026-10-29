using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Constants;
using MotoRent.WebApp.Models;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Controllers;

/// <summary>Lịch sử thanh toán của khách hàng đang đăng nhập.</summary>
[Authorize(Roles = AppRoles.Customer)]
public class PaymentsController : Controller
{
    private readonly PaymentApiClient _api;

    public PaymentsController(PaymentApiClient api)
    {
        _api = api;
    }

    // GET /Payments/My
    public async Task<IActionResult> My()
    {
        var result = await _api.GetMyAsync();
        var model = new MyPaymentsViewModel();

        if (result.Success) model.Payments = result.Data ?? new();
        else model.ErrorMessage = result.Message;

        return View(model);
    }
}
