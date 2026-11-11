namespace MotoRent.BusinessObjects.DTOs.Rentals;

/// <summary>
/// Thông tin tóm tắt đơn thuê — kiểu dữ liệu của OData endpoint /odata/Rentals (Admin/Staff tra cứu).
/// </summary>
public class RentalSummaryDto
{
    public int RentalId { get; set; }
    public string RentalCode { get; set; } = null!;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string? CustomerPhone { get; set; }
    public int? StaffId { get; set; }
    public string? StaffName { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public string Status { get; set; } = null!;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LateFee { get; set; }
    public decimal DamageFee { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public int MotorbikeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
