namespace MotoRent.BusinessObjects.Helpers;

/// <summary>
/// Kiểm tra thời điểm có nằm trong giờ mở cửa của chi nhánh không (quy tắc BR12).
/// Giờ mở cửa lấy từ Branches.OpenTime / CloseTime — mỗi chi nhánh một khung giờ riêng.
/// </summary>
public static class BranchHours
{
    /// <summary>Thời điểm có nằm trong [open, close] của chi nhánh không (so theo giờ:phút).</summary>
    public static bool IsWithin(TimeOnly open, TimeOnly close, DateTime moment)
    {
        var t = TimeOnly.FromDateTime(moment);
        return t >= open && t <= close;
    }

    /// <summary>VD: "07:00 – 21:00".</summary>
    public static string Format(TimeOnly open, TimeOnly close) => $"{open.ToString("HH:mm")} – {close.ToString("HH:mm")}";

    /// <summary>Thông báo lỗi chuẩn khi giờ nằm ngoài khung mở cửa; trả null nếu hợp lệ.</summary>
    public static string? Validate(string branchName, TimeOnly open, TimeOnly close, DateTime moment, string label)
    {
        if (IsWithin(open, close, moment)) return null;
        return $"{label} ({moment:HH:mm}) nằm ngoài giờ mở cửa của chi nhánh '{branchName}' ({Format(open, close)}). Vui lòng chọn giờ trong khung này.";
    }
}
