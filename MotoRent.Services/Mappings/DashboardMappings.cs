using System.Linq.Expressions;
using MotoRent.BusinessObjects.DTOs.Dashboard;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.Services.Mappings;

public static class DashboardMappings
{
    public static readonly Expression<Func<VwMonthlyRevenue, MonthlyRevenueDto>> MonthlyRevenueToDto = v => new MonthlyRevenueDto
    {
        Year = v.Year ?? 0,
        Month = v.Month ?? 0,
        RentalRevenue = v.RentalRevenue ?? 0m,
        PenaltyRevenue = v.PenaltyRevenue ?? 0m,
        TotalRevenue = v.TotalRevenue ?? 0m,
        RentalCount = v.RentalCount ?? 0
    };

    public static readonly Expression<Func<VwMotorbikeStatistic, MotorbikeStatisticDto>> MotorbikeStatisticToDto = v => new MotorbikeStatisticDto
    {
        MotorbikeId = v.MotorbikeId,
        LicensePlate = v.LicensePlate,
        ModelName = v.ModelName,
        BrandName = v.BrandName,
        CategoryName = v.CategoryName,
        BranchName = v.BranchName,
        PricePerDay = v.PricePerDay,
        Status = v.Status,
        CompletedRentals = v.CompletedRentals ?? 0,
        TotalEarned = v.TotalEarned ?? 0m,
        AverageRating = (double?)v.AverageRating
    };
}
