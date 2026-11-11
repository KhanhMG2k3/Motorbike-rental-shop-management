using MotoRent.BusinessObjects.DTOs.Dashboard;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi API Dashboard và thống kê báo cáo (Admin).
/// </summary>
public class DashboardApiClient : ApiClientBase
{
    public DashboardApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<DashboardSummaryDto>> GetSummaryAsync(int? year = null, int? month = null)
    {
        var query = new List<string>();
        if (year.HasValue) query.Add($"year={year.Value}");
        if (month.HasValue) query.Add($"month={month.Value}");
        var url = "api/dashboard/summary" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return GetAsync<DashboardSummaryDto>(url);
    }

    public Task<ApiResult<List<MonthlyRevenueDto>>> GetMonthlyRevenueAsync(int? year = null)
    {
        var url = "api/dashboard/monthly-revenue" + (year.HasValue ? $"?year={year.Value}" : "");
        return GetAsync<List<MonthlyRevenueDto>>(url);
    }

    public Task<ApiResult<List<MotorbikeStatisticDto>>> GetTopMotorbikesAsync(int top = 10)
        => GetAsync<List<MotorbikeStatisticDto>>($"api/dashboard/top-motorbikes?top={top}");
}
