using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.API.Extensions;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Payments;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

/// <summary>
/// Thanh toán. Các khoản cọc / tiền thuê / phạt / hoàn cọc được tạo tự động khi duyệt, trả xe, huỷ đơn
/// (RentalsController). Controller này dùng để tra cứu và xử lý các khoản đang chờ.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Tra cứu thanh toán: lọc theo đơn / loại / phương thức / trạng thái / ngày, có phân trang và tổng tiền (Admin, Staff).</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public async Task<ActionResult<ApiResponse<PaymentListDto>>> GetList([FromQuery] PaymentQuery query)
        => Ok(ApiResponse<PaymentListDto>.Ok(await _paymentService.GetListAsync(query)));

    /// <summary>Lịch sử thanh toán của tôi (Customer).</summary>
    [HttpGet("my")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<ApiResponse<List<PaymentDto>>>> GetMy()
        => Ok(ApiResponse<List<PaymentDto>>.Ok(await _paymentService.GetMyAsync(RequireUserId())));

    /// <summary>Xác nhận đã thu tiền một khoản đang chờ: Pending → Paid (Admin, Staff).</summary>
    [HttpPut("{id:int}/confirm")]
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Confirm(int id, [FromBody] ConfirmPaymentDto dto)
        => Ok(ApiResponse<PaymentDto>.Ok(await _paymentService.ConfirmAsync(id, dto, RequireUserId()), "Đã xác nhận thanh toán."));

    /// <summary>Huỷ một khoản đang chờ: Pending → Cancelled, bắt buộc có lý do (Admin, Staff).</summary>
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Cancel(int id, [FromBody] CancelPaymentDto dto)
        => Ok(ApiResponse<PaymentDto>.Ok(await _paymentService.CancelAsync(id, dto, RequireUserId()), "Đã huỷ khoản thanh toán."));

    private int RequireUserId()
        => User.GetUserId() ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
}
