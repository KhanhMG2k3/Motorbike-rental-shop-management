using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.BusinessObjects.DTOs.Motorbikes;

namespace MotoRent.WebApp.Models;

/// <summary>Bộ lọc trang danh sách xe (bind từ query string).</summary>
public class MotorbikeFilterViewModel
{
    [Display(Name = "Từ khoá")]
    [StringLength(50)]
    public string? Keyword { get; set; }

    [Display(Name = "Hãng xe")]
    public int? BrandId { get; set; }

    [Display(Name = "Loại xe")]
    public int? CategoryId { get; set; }

    [Display(Name = "Giá từ")]
    [Range(0, 10000000, ErrorMessage = "Giá không hợp lệ.")]
    public decimal? MinPrice { get; set; }

    [Display(Name = "Giá đến")]
    [Range(0, 10000000, ErrorMessage = "Giá không hợp lệ.")]
    public decimal? MaxPrice { get; set; }

    [Display(Name = "Chỉ xe sẵn sàng")]
    public bool OnlyAvailable { get; set; }

    [Display(Name = "Sắp xếp")]
    public string Sort { get; set; } = "price_asc";

    public int Page { get; set; } = 1;
}

public class MotorbikeListViewModel
{
    public MotorbikeFilterViewModel Filter { get; set; } = new();
    public List<MotorbikeDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public List<SelectListItem> BrandOptions { get; set; } = new();
    public List<SelectListItem> CategoryOptions { get; set; } = new();

    public static readonly List<SelectListItem> SortOptions = new()
    {
        new("Giá thấp → cao", "price_asc"),
        new("Giá cao → thấp", "price_desc"),
        new("Xe mới nhất", "newest"),
        new("Đánh giá cao", "rating")
    };

    public string? ErrorMessage { get; set; }
}

public class MotorbikeDetailViewModel
{
    public MotorbikeDto Motorbike { get; set; } = null!;
    public List<MotorbikeDto> SimilarMotorbikes { get; set; } = new();

    [Display(Name = "Ngày nhận xe")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Ngày trả xe")]
    public DateTime? EndDate { get; set; }

    /// <summary>null = chưa kiểm tra.</summary>
    public bool? IsAvailable { get; set; }
    public string? AvailabilityError { get; set; }

    public int RentalDays => StartDate.HasValue && EndDate.HasValue && EndDate > StartDate
        ? (int)Math.Ceiling((EndDate.Value - StartDate.Value).TotalDays)
        : 0;

    public decimal EstimatedPrice => RentalDays * Motorbike.PricePerDay;
}

/// <summary>Model cho partial _Pagination (dùng chung cho mọi trang có phân trang).</summary>
public class PaginationViewModel
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public string Controller { get; set; } = null!;
    public string Action { get; set; } = null!;

    /// <summary>Các tham số lọc hiện tại, giữ nguyên khi chuyển trang.</summary>
    public Dictionary<string, string> RouteValues { get; set; } = new();

    public Dictionary<string, string> RouteFor(int page)
        => new(RouteValues) { ["page"] = page.ToString() };
}
