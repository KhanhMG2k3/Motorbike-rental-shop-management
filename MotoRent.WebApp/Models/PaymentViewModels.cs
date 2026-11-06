using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Payments;
using MotoRent.WebApp.Helpers;

namespace MotoRent.WebApp.Models;

/// <summary>Trang Thanh toán của Admin / Staff: bộ lọc + kết quả + tổng tiền.</summary>
public class PaymentListViewModel
{
    public PaymentQuery Filter { get; set; } = new();
    public PaymentListDto Result { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public int TotalPages => Result.PageSize == 0 ? 0 : (int)Math.Ceiling(Result.TotalCount / (double)Result.PageSize);

    public static List<SelectListItem> TypeOptions() => new[]
    {
        PaymentType.Deposit, PaymentType.RentalFee, PaymentType.Penalty, PaymentType.Refund
    }.Select(t => new SelectListItem(DisplayHelper.PaymentTypeText(t), t)).ToList();

    public static List<SelectListItem> MethodOptions() => new[]
    {
        PaymentMethod.Cash, PaymentMethod.BankTransfer, PaymentMethod.Momo, PaymentMethod.VNPay
    }.Select(m => new SelectListItem(DisplayHelper.PaymentMethodText(m), m)).ToList();

    public static List<SelectListItem> StatusOptions() => new[]
    {
        PaymentStatus.Pending, PaymentStatus.Paid, PaymentStatus.Failed, PaymentStatus.Cancelled
    }.Select(s => new SelectListItem(DisplayHelper.PaymentStatusText(s), s)).ToList();
}

/// <summary>Trang Lịch sử thanh toán của khách hàng.</summary>
public class MyPaymentsViewModel
{
    public List<PaymentDto> Payments { get; set; } = new();
    public string? ErrorMessage { get; set; }

    /// <summary>Tiền khách đã trả (cọc + tiền thuê + phạt, chỉ khoản đã thanh toán).</summary>
    public decimal TotalPaid => Payments.Where(p => p.Status == PaymentStatus.Paid && p.PaymentType != PaymentType.Refund).Sum(p => p.Amount);

    /// <summary>Tiền đã được hoàn lại (hoàn cọc).</summary>
    public decimal TotalRefunded => Payments.Where(p => p.Status == PaymentStatus.Paid && p.PaymentType == PaymentType.Refund).Sum(p => p.Amount);

    public int PendingCount => Payments.Count(p => p.Status == PaymentStatus.Pending);
}
