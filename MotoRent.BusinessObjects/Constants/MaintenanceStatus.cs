namespace MotoRent.BusinessObjects.Constants;

/// <summary>
/// Giá trị cột MaintenanceRecords.Status (khớp CHECK constraint trong DB).
/// Scheduled → InProgress → Completed, hoặc → Cancelled.
/// </summary>
public static class MaintenanceStatus
{
    public const string Scheduled = "Scheduled";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Scheduled, InProgress, Completed, Cancelled };

    /// <summary>Phiếu còn mở (chưa xong / chưa huỷ).</summary>
    public static readonly string[] Open = { Scheduled, InProgress };
}
