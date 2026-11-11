namespace MotoRent.BusinessObjects.Constants;

/// <summary>
/// Giá trị cột Rentals.Status (khớp CHECK constraint trong DB).
/// </summary>
public static class RentalStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Renting = "Renting";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    /// <summary>Các trạng thái đang "giữ" xe — dùng khi kiểm tra trùng lịch.</summary>
    public static readonly string[] Active = { Pending, Confirmed, Renting };
}
