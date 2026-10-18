using MotoRent.BusinessObjects.DTOs.Dashboard;

namespace MotoRent.WebApp.Areas.Admin.Models;

public class DashboardViewModel
{
    public DashboardSummaryDto Summary { get; set; } = new();
    public List<MonthlyRevenueDto> MonthlyRevenues { get; set; } = new();
    public List<MotorbikeStatisticDto> TopMotorbikes { get; set; } = new();
    public int SelectedYear { get; set; }
}
