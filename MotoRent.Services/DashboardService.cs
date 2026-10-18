using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Dashboard;
using MotoRent.DataAccess;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

public class DashboardService : IDashboardService
{
    private readonly MotorbikeRentalContext _context;

    public DashboardService(MotorbikeRentalContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var totalBikes = await _context.Motorbikes.CountAsync();
        var availableBikes = await _context.Motorbikes.CountAsync(m => m.Status == MotorbikeStatus.Available);
        var rentedBikes = await _context.Motorbikes.CountAsync(m => m.Status == MotorbikeStatus.Rented);
        var maintenanceBikes = await _context.Motorbikes.CountAsync(m => m.Status == MotorbikeStatus.Maintenance);

        var totalCustomers = await _context.Users.CountAsync(u => u.RoleId == 3);
        var totalStaff = await _context.Users.CountAsync(u => u.RoleId == 2 && u.IsActive);
        var completedRentals = await _context.Rentals.CountAsync(r => r.Status == RentalStatus.Completed);

        var totalRevenue = await _context.VwMonthlyRevenues.SumAsync(v => (decimal?)v.TotalRevenue) ?? 0m;

        var now = DateTime.Now;
        var thisMonthRecord = await _context.VwMonthlyRevenues
            .FirstOrDefaultAsync(v => v.Year == now.Year && v.Month == now.Month);
        var thisMonthRevenue = thisMonthRecord?.TotalRevenue ?? 0m;

        return new DashboardSummaryDto
        {
            TotalMotorbikes = totalBikes,
            AvailableMotorbikes = availableBikes,
            RentedMotorbikes = rentedBikes,
            MaintenanceMotorbikes = maintenanceBikes,
            TotalCustomers = totalCustomers,
            TotalStaff = totalStaff,
            CompletedRentals = completedRentals,
            TotalRevenue = totalRevenue,
            ThisMonthRevenue = thisMonthRevenue
        };
    }

    public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenuesAsync(int? year = null)
    {
        var targetYear = year ?? DateTime.Now.Year;
        return await _context.VwMonthlyRevenues
            .Where(v => v.Year == targetYear)
            .OrderBy(v => v.Month)
            .Select(DashboardMappings.MonthlyRevenueToDto)
            .ToListAsync();
    }

    public async Task<List<MotorbikeStatisticDto>> GetTopMotorbikesAsync(int top = 10)
    {
        var limit = Math.Clamp(top, 1, 50);
        return await _context.VwMotorbikeStatistics
            .OrderByDescending(v => v.TotalEarned)
            .ThenByDescending(v => v.CompletedRentals)
            .Take(limit)
            .Select(DashboardMappings.MotorbikeStatisticToDto)
            .ToListAsync();
    }
}
