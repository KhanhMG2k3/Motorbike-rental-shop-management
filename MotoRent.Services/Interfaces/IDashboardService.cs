using MotoRent.BusinessObjects.DTOs.Dashboard;

namespace MotoRent.Services.Interfaces;

public interface IDashboardService
{
    /// <summary>Số liệu tổng quan. "Doanh thu tháng" lấy theo year/month truyền vào (mặc định: tháng/năm hiện tại).</summary>
    Task<DashboardSummaryDto> GetSummaryAsync(int? year = null, int? month = null);
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenuesAsync(int? year = null);
    Task<List<MotorbikeStatisticDto>> GetTopMotorbikesAsync(int top = 10);
}
