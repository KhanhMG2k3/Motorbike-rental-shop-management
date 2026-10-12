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
}
