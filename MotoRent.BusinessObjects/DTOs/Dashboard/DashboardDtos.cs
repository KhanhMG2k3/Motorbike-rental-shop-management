namespace MotoRent.BusinessObjects.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalMotorbikes { get; set; }
    public int AvailableMotorbikes { get; set; }
    public int RentedMotorbikes { get; set; }
    public int MaintenanceMotorbikes { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalStaff { get; set; }
    public int CompletedRentals { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal ThisMonthRevenue { get; set; }
}

public class MonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal RentalRevenue { get; set; }
    public decimal PenaltyRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public int RentalCount { get; set; }
}

public class MotorbikeStatisticDto
{
    public int MotorbikeId { get; set; }
    public string LicensePlate { get; set; } = null!;
    public string ModelName { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public decimal PricePerDay { get; set; }
    public string Status { get; set; } = null!;
    public int CompletedRentals { get; set; }
    public decimal TotalEarned { get; set; }
    public double? AverageRating { get; set; }
}
