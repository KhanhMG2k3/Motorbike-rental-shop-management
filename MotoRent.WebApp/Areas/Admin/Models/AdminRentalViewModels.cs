using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Branches;
using MotoRent.BusinessObjects.DTOs.Rentals;

namespace MotoRent.WebApp.Areas.Admin.Models;

/// <summary>Bộ lọc tra cứu danh sách đơn thuê qua OData (/odata/Rentals).</summary>
public class AdminRentalFilter
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int? BranchId { get; set; }
    public int Page { get; set; } = 1;
}

public class AdminRentalListViewModel
{
    public AdminRentalFilter Filter { get; set; } = new();
    public List<RentalSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public List<SelectListItem> BranchOptions { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public static List<SelectListItem> StatusOptions() => new()
    {
        new("Chờ duyệt (Pending)", RentalStatus.Pending),
        new("Đã xác nhận - Chờ giao (Confirmed)", RentalStatus.Confirmed),
        new("Đang thuê - Chờ trả (Renting)", RentalStatus.Renting),
        new("Hoàn thành (Completed)", RentalStatus.Completed),
        new("Đã huỷ (Cancelled)", RentalStatus.Cancelled)
    };

    public static List<SelectListItem> PaymentMethodOptions() => new()
    {
        new("Tiền mặt tại quầy (Cash)", PaymentMethod.Cash),
        new("Chuyển khoản ngân hàng (BankTransfer)", PaymentMethod.BankTransfer),
        new("Ví MoMo", PaymentMethod.Momo),
        new("VNPay", PaymentMethod.VNPay)
    };
}

/// <summary>ViewModel trang Chi tiết đơn thuê & Duyệt cọc / Huỷ đơn dành cho Staff/Admin.</summary>
public class StaffRentalDetailViewModel
{
    public RentalDto Rental { get; set; } = null!;
    public ConfirmRentalDto ConfirmForm { get; set; } = new();
    public CancelRentalDto CancelForm { get; set; } = new();

    public bool HasValidDocuments =>
        !string.IsNullOrWhiteSpace(Rental.CustomerIdentityNumber) &&
        !string.IsNullOrWhiteSpace(Rental.CustomerDriverLicense);
}

/// <summary>ViewModel trang Biên bản bàn giao xe (Confirmed → Renting).</summary>
public class StaffPickupViewModel
{
    public RentalDto Rental { get; set; } = null!;
    public PickupRentalDto Form { get; set; } = new();

    /// <summary>Số km hiện tại trên hệ thống của từng xe (MotorbikeId → Odometer).</summary>
    public Dictionary<int, int> CurrentOdometers { get; set; } = new();

    public bool HasValidDocuments =>
        !string.IsNullOrWhiteSpace(Rental.CustomerIdentityNumber) &&
        !string.IsNullOrWhiteSpace(Rental.CustomerDriverLicense);
}

/// <summary>ViewModel trang Nhận trả xe & Quyết toán (Renting → Completed).</summary>
public class StaffReturnViewModel
{
    public RentalDto Rental { get; set; } = null!;
    public ReturnRentalDto Form { get; set; } = new();
}

/// <summary>ViewModel trang Hoá đơn quyết toán in ấn (Printable Settlement Invoice).</summary>
public class StaffInvoiceViewModel
{
    public RentalDto Rental { get; set; } = null!;
    public BranchDto? Branch { get; set; }
}