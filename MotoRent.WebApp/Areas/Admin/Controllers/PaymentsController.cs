using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Payments;
using MotoRent.WebApp.Models;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

/// <summary>Tra cứu thanh toán, xác nhận / huỷ các khoản đang chờ (Admin, Staff).</summary>
[Area("Admin")]
[Authorize(Roles = AppRoles.AdminOrStaff)]
public class PaymentsController : Controller
{
    private const int PageSize = 10;

    private readonly PaymentApiClient _api;

    public PaymentsController(PaymentApiClient api)
    {
        _api = api;
    }

    // GET /Admin/Payments?type=&method=&status=&from=&to=&rentalId=&page=
    public async Task<IActionResult> Index([FromQuery] PaymentQuery filter)
    {
        filter.PageSize = PageSize;
        var model = new PaymentListViewModel { Filter = filter };

        if (ModelState.IsValid)
        {
            var result = await _api.GetListAsync(filter);
            if (result.Success) model.Result = result.Data!;
            else model.ErrorMessage = result.Message;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, string? transactionRef, string? returnUrl)
    {
        var result = await _api.ConfirmAsync(id, new ConfirmPaymentDto { TransactionRef = transactionRef });
        SetResultMessage(result);
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, string? returnUrl)
    {
        var result = await _api.CancelAsync(id, new CancelPaymentDto { Reason = reason ?? string.Empty });
        SetResultMessage(result);
        return RedirectBack(returnUrl);
    }

    private void SetResultMessage(ApiResult<PaymentDto> result)
    {
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            // Ưu tiên lỗi validate cụ thể từng trường (nếu có), không thì dùng thông báo chung
            TempData["Error"] = result.Errors?.Values.SelectMany(e => e).FirstOrDefault() ?? result.Message;
        }
    }

    private IActionResult RedirectBack(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
}
