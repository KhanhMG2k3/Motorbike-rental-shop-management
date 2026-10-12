using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.DTOs.Brands;
using MotoRent.BusinessObjects.DTOs.Categories;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.WebApp.Models;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi các API Catalog (xe, hãng, loại xe). Các API này là Public nên chưa cần JWT.
/// </summary>
public class MotorbikeApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public MotorbikeApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Tìm xe qua OData: ghép $filter, $orderby, $top, $skip, $count từ bộ lọc.</summary>
    public async Task<(List<MotorbikeDto> Items, int TotalCount)> SearchAsync(MotorbikeFilterViewModel filter, int pageSize)
    {
        var url = "odata/Motorbikes?" + string.Join("&", new[]
        {
            "$filter=" + Uri.EscapeDataString(BuildODataFilter(filter)),
            "$orderby=" + Uri.EscapeDataString(BuildODataOrderBy(filter.Sort)),
            "$top=" + pageSize,
            "$skip=" + (Math.Max(filter.Page, 1) - 1) * pageSize,
            "$count=true"
        });

        var result = await _http.GetFromJsonAsync<ODataResponse<MotorbikeDto>>(url, JsonOptions)
                     ?? new ODataResponse<MotorbikeDto>();

        result.Value.ForEach(ResolveImageUrl);
        return (result.Value, result.Count ?? result.Value.Count);
    }

    /// <summary>Xe cùng loại (gợi ý ở trang chi tiết).</summary>
    public async Task<List<MotorbikeDto>> GetSimilarAsync(int categoryId, int excludeId, int take = 4)
    {
        var filter = $"CategoryId eq {categoryId} and MotorbikeId ne {excludeId} and Status eq 'Available'";
        var url = $"odata/Motorbikes?$filter={Uri.EscapeDataString(filter)}&$orderby=PricePerDay&$top={take}";

        var result = await _http.GetFromJsonAsync<ODataResponse<MotorbikeDto>>(url, JsonOptions);
        var items = result?.Value ?? new List<MotorbikeDto>();
        items.ForEach(ResolveImageUrl);
        return items;
    }

    public async Task<MotorbikeDto?> GetByIdAsync(int id)
    {
        var response = await _http.GetAsync($"api/motorbikes/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MotorbikeDto>>(JsonOptions);
        if (body?.Data != null) ResolveImageUrl(body.Data);
        return body?.Data;
    }

    /// <summary>
    /// Kiểm tra 1 xe có trống trong khoảng thời gian không (gọi API /api/motorbikes/available).
    /// Trả về (true/false, null) hoặc (false, thông báo lỗi validate từ API).
    /// </summary>
    public async Task<(bool IsAvailable, string? Error)> CheckAvailabilityAsync(MotorbikeDto bike, DateTime start, DateTime end)
    {
        var url = "api/motorbikes/available"
                  + $"?startDate={Uri.EscapeDataString(start.ToString("s", CultureInfo.InvariantCulture))}"
                  + $"&endDate={Uri.EscapeDataString(end.ToString("s", CultureInfo.InvariantCulture))}"
                  + $"&branchId={bike.BranchId}&categoryId={bike.CategoryId}";

        var response = await _http.GetAsync(url);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<MotorbikeDto>>>(JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var firstError = body?.Errors?.Values.SelectMany(e => e).FirstOrDefault();
            return (false, firstError ?? body?.Message ?? "Không kiểm tra được lịch xe.");
        }

        var isAvailable = body?.Data?.Any(m => m.MotorbikeId == bike.MotorbikeId) == true;
        return (isAvailable, null);
    }

    public async Task<List<BrandDto>> GetBrandsAsync()
    {
        var body = await _http.GetFromJsonAsync<ApiResponse<List<BrandDto>>>("api/brands", JsonOptions);
        return body?.Data ?? new List<BrandDto>();
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var body = await _http.GetFromJsonAsync<ApiResponse<List<CategoryDto>>>("api/categories", JsonOptions);
        return body?.Data ?? new List<CategoryDto>();
    }

    private static string BuildODataFilter(MotorbikeFilterViewModel f)
    {
        var parts = new List<string>
        {
            f.OnlyAvailable ? "Status eq 'Available'" : "Status ne 'Inactive'"
        };

        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            // Escape dấu nháy đơn theo chuẩn OData: ' → ''
            var kw = f.Keyword.Trim().ToLower().Replace("'", "''");
            parts.Add($"(contains(tolower(ModelName),'{kw}') or contains(tolower(BrandName),'{kw}') or contains(tolower(LicensePlate),'{kw}'))");
        }

        if (f.BrandId.HasValue) parts.Add($"BrandId eq {f.BrandId}");
        if (f.CategoryId.HasValue) parts.Add($"CategoryId eq {f.CategoryId}");
        if (f.MinPrice.HasValue) parts.Add($"PricePerDay ge {f.MinPrice.Value.ToString(CultureInfo.InvariantCulture)}");
        if (f.MaxPrice.HasValue) parts.Add($"PricePerDay le {f.MaxPrice.Value.ToString(CultureInfo.InvariantCulture)}");

        return string.Join(" and ", parts);
    }

    private static string BuildODataOrderBy(string? sort) => sort switch
    {
        "price_desc" => "PricePerDay desc",
        "newest" => "ManufactureYear desc,PricePerDay",
        "rating" => "AverageRating desc,PricePerDay",
        _ => "PricePerDay asc"
    };

    /// <summary>Ảnh xe lưu ở API (wwwroot/images/...) → ghép thành URL tuyệt đối.</summary>
    private void ResolveImageUrl(MotorbikeDto bike)
    {
        if (!string.IsNullOrEmpty(bike.ImageUrl) && bike.ImageUrl.StartsWith('/'))
            bike.ImageUrl = new Uri(_http.BaseAddress!, bike.ImageUrl).ToString();
    }

    private class ODataResponse<T>
    {
        [JsonPropertyName("@odata.count")]
        public int? Count { get; set; }

        [JsonPropertyName("value")]
        public List<T> Value { get; set; } = new();
    }
}
