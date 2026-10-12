namespace MotoRent.BusinessObjects.Constants;

/// <summary>
/// Giá trị cột Motorbikes.Status (khớp CHECK constraint trong DB).
/// </summary>
public static class MotorbikeStatus
{
    public const string Available = "Available";
    public const string Rented = "Rented";
    public const string Maintenance = "Maintenance";
    public const string Inactive = "Inactive";

    public static readonly string[] All = { Available, Rented, Maintenance, Inactive };
}
