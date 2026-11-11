using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Maintenances;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.WebApp.Helpers;

namespace MotoRent.WebApp.Areas.Admin.Models;

public static class AdminOptions
{
    public static List<SelectListItem> MotorbikeStatuses() => MotorbikeStatus.All
        .Select(s => new SelectListItem(DisplayHelper.MotorbikeStatusText(s), s)).ToList();

    /// <summary>Trạng thái được đổi tay (Rented chỉ do luồng thuê xe).</summary>
    public static List<SelectListItem> SettableMotorbikeStatuses() =>
        new[] { MotorbikeStatus.Available, MotorbikeStatus.Maintenance, MotorbikeStatus.Inactive }
            .Select(s => new SelectListItem(DisplayHelper.MotorbikeStatusText(s), s)).ToList();

    public static List<SelectListItem> MaintenanceStatuses() => MaintenanceStatus.All
        .Select(s => new SelectListItem(DisplayHelper.MaintenanceStatusText(s), s)).ToList();
}

// ===== Xe máy =====
public class AdminMotorbikeFilter
{
    [StringLength(50)]
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int? BranchId { get; set; }
    public int? BrandId { get; set; }
    public int Page { get; set; } = 1;
}

public class AdminMotorbikeListViewModel
{
    public AdminMotorbikeFilter Filter { get; set; } = new();
    public List<MotorbikeDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public List<SelectListItem> BranchOptions { get; set; } = new();
    public List<SelectListItem> BrandOptions { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class MotorbikeFormViewModel
{
    /// <summary>null = thêm mới.</summary>
    public int? MotorbikeId { get; set; }
    public UpdateMotorbikeDto Form { get; set; } = new();

    /// <summary>Thông tin hiện tại (ảnh, trạng thái) — chỉ có khi sửa.</summary>
    public MotorbikeDto? Current { get; set; }
    public List<MaintenanceDto> Maintenances { get; set; } = new();

    public List<SelectListItem> BrandOptions { get; set; } = new();
    public List<SelectListItem> CategoryOptions { get; set; } = new();
    public List<SelectListItem> BranchOptions { get; set; } = new();
}

// ===== Bảo dưỡng =====
public class MaintenanceListViewModel
{
    public MaintenanceQuery Filter { get; set; } = new();
    public List<MaintenanceDto> Items { get; set; } = new();
    public List<SelectListItem> BranchOptions { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class MaintenanceFormViewModel
{
    /// <summary>null = lên lịch mới.</summary>
    public int? MaintenanceId { get; set; }
    public CreateMaintenanceDto Form { get; set; } = new();
    public MaintenanceDto? Current { get; set; }
    public List<SelectListItem> MotorbikeOptions { get; set; } = new();
}

public class MaintenanceCompleteViewModel
{
    public MaintenanceDto Current { get; set; } = null!;
    public CompleteMaintenanceDto Form { get; set; } = new();
}
