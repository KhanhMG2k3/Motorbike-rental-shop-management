using MotoRent.BusinessObjects.DTOs.Dashboard;

namespace MotoRent.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenuesAsync(int? year = null);
    Task<List<MotorbikeStatisticDto>> GetTopMotorbikesAsync(int top = 10);
}
