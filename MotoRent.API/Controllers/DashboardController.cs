using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Dashboard;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Số liệu thống kê tổng quan (KPIs: xe, đơn thuê, khách hàng, doanh thu).</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary()
    {
        var summary = await _dashboardService.GetSummaryAsync();
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary));
    }

    /// <summary>Doanh thu theo từng tháng trong năm (mặc định năm hiện tại).</summary>
    [HttpGet("monthly-revenue")]
    public async Task<ActionResult<ApiResponse<List<MonthlyRevenueDto>>>> GetMonthlyRevenue([FromQuery] int? year)
    {
        var revenues = await _dashboardService.GetMonthlyRevenuesAsync(year);
        return Ok(ApiResponse<List<MonthlyRevenueDto>>.Ok(revenues));
    }

    /// <summary>Top xe có doanh thu và lượt thuê cao nhất.</summary>
    [HttpGet("top-motorbikes")]
    public async Task<ActionResult<ApiResponse<List<MotorbikeStatisticDto>>>> GetTopMotorbikes([FromQuery] int top = 10)
    {
        var stats = await _dashboardService.GetTopMotorbikesAsync(top);
        return Ok(ApiResponse<List<MotorbikeStatisticDto>>.Ok(stats));
    }
}
