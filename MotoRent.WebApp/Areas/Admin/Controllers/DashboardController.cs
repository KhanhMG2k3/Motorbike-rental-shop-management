using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Constants;
using MotoRent.WebApp.Areas.Admin.Models;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private readonly DashboardApiClient _dashboardApi;

    public DashboardController(DashboardApiClient dashboardApi)
    {
        _dashboardApi = dashboardApi;
    }

    public async Task<IActionResult> Index([FromQuery] int? year, [FromQuery] int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = Math.Clamp(month ?? DateTime.Now.Month, 1, 12);

        var summaryRes = await _dashboardApi.GetSummaryAsync(targetYear, targetMonth);
        var monthlyRes = await _dashboardApi.GetMonthlyRevenueAsync(targetYear);
        var topBikesRes = await _dashboardApi.GetTopMotorbikesAsync(10);

        if (!summaryRes.Success) ViewData["Error"] = summaryRes.Message;

        var vm = new DashboardViewModel
        {
            Summary = summaryRes.Data ?? new(),
            MonthlyRevenues = monthlyRes.Data ?? new(),
            TopMotorbikes = topBikesRes.Data ?? new(),
            SelectedYear = targetYear,
            SelectedMonth = targetMonth
        };

        return View(vm);
    }
}
