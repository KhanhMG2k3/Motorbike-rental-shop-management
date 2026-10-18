using MotoRent.BusinessObjects.DTOs.Dashboard;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi API Dashboard và thống kê báo cáo (Admin).
/// </summary>
public class DashboardApiClient : ApiClientBase
{
    public DashboardApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<DashboardSummaryDto>> GetSummaryAsync()
        => GetAsync<DashboardSummaryDto>("api/dashboard/summary");

    public Task<ApiResult<List<MonthlyRevenueDto>>> GetMonthlyRevenueAsync(int? year = null)
    {
        var url = "api/dashboard/monthly-revenue" + (year.HasValue ? $"?year={year.Value}" : "");
        return GetAsync<List<MonthlyRevenueDto>>(url);
    }

    public Task<ApiResult<List<MotorbikeStatisticDto>>> GetTopMotorbikesAsync(int top = 10)
        => GetAsync<List<MotorbikeStatisticDto>>($"api/dashboard/top-motorbikes?top={top}");
}
