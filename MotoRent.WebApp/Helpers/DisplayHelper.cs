using System.Globalization;
using MotoRent.BusinessObjects.Constants;

namespace MotoRent.WebApp.Helpers;

public static class DisplayHelper
{
    private static readonly CultureInfo Vi = new("vi-VN");

    public static string Money(decimal amount) => amount.ToString("N0", Vi) + " đ";

    public static string MotorbikeStatusText(string status) => status switch
    {
        MotorbikeStatus.Available => "Sẵn sàng",
        MotorbikeStatus.Rented => "Đang cho thuê",
        MotorbikeStatus.Maintenance => "Đang bảo dưỡng",
        MotorbikeStatus.Inactive => "Ngừng cho thuê",
        _ => status
    };

    /// <summary>Class màu Bootstrap cho badge trạng thái xe.</summary>
    public static string MotorbikeStatusBadge(string status) => status switch
    {
        MotorbikeStatus.Available => "bg-success",
        MotorbikeStatus.Rented => "bg-warning text-dark",
        MotorbikeStatus.Maintenance => "bg-secondary",
        _ => "bg-dark"
    };

    public static string EngineText(int? cc) => cc.HasValue ? $"{cc} cc" : "Xe điện";

    public static string MaintenanceStatusText(string status) => status switch
    {
        MaintenanceStatus.Scheduled => "Chờ bảo dưỡng",
        MaintenanceStatus.InProgress => "Đang bảo dưỡng",
        MaintenanceStatus.Completed => "Hoàn thành",
        MaintenanceStatus.Cancelled => "Đã huỷ",
        _ => status
    };

    public static string MaintenanceStatusBadge(string status) => status switch
    {
        MaintenanceStatus.Scheduled => "bg-info text-dark",
        MaintenanceStatus.InProgress => "bg-warning text-dark",
        MaintenanceStatus.Completed => "bg-success",
        _ => "bg-secondary"
    };

    public static string RentalStatusText(string status) => status switch
    {
        RentalStatus.Pending => "Chờ duyệt",
        RentalStatus.Confirmed => "Đã xác nhận",
        RentalStatus.Renting => "Đang thuê",
        RentalStatus.Completed => "Hoàn thành",
        RentalStatus.Cancelled => "Đã huỷ",
        _ => status
    };

    public static string RentalStatusBadge(string status) => status switch
    {
        RentalStatus.Pending => "bg-warning text-dark",
        RentalStatus.Confirmed => "bg-info text-dark",
        RentalStatus.Renting => "bg-primary",
        RentalStatus.Completed => "bg-success",
        RentalStatus.Cancelled => "bg-secondary",
        _ => "bg-dark"
    };

    public static string PaymentTypeText(string type) => type switch
    {
        PaymentType.Deposit => "Tiền cọc",
        PaymentType.RentalFee => "Tiền thuê xe",
        PaymentType.Penalty => "Phí phạt (trễ / hư hỏng)",
        PaymentType.Refund => "Hoàn trả tiền cọc",
        _ => type
    };

    public static string PaymentMethodText(string method) => method switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.Momo => "Ví MoMo",
        PaymentMethod.VNPay => "VNPay",
        _ => method
    };

    public static string Date(DateOnly? date) => date?.ToString("dd/MM/yyyy") ?? "—";

    public static string DateTimeText(DateTime? dt) => dt?.ToString("dd/MM/yyyy HH:mm") ?? "—";
}