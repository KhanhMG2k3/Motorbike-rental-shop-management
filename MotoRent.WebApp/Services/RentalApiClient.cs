using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.BusinessObjects.DTOs.Rentals;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi API Đơn thuê xe (/api/rentals & /odata/Rentals) cho Customer và Staff/Admin.
/// </summary>
public class RentalApiClient : ApiClientBase
{
    public RentalApiClient(HttpClient http) : base(http) { }

    /// <summary>Tra cứu danh sách đơn thuê qua OData (dành cho Staff / Admin).</summary>
    public Task<ApiResult<PagedResult<RentalSummaryDto>>> SearchODataAsync(
        string? keyword, string? status, int? branchId, int page, int pageSize)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower().Replace("'", "''");
            conditions.Add($"(contains(tolower(RentalCode),'{kw}') or contains(tolower(CustomerName),'{kw}') or (CustomerPhone ne null and contains(CustomerPhone,'{kw}')))");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add($"Status eq '{status.Replace("'", "''")}'");
        }

        if (branchId.HasValue)
        {
            conditions.Add($"BranchId eq {branchId.Value}");
        }

        var query = new List<string>
        {
            "$orderby=" + Uri.EscapeDataString("CreatedAt desc"),
            "$top=" + pageSize,
            "$skip=" + (Math.Max(page, 1) - 1) * pageSize,
            "$count=true"
        };

        if (conditions.Count > 0)
        {
            query.Insert(0, "$filter=" + Uri.EscapeDataString(string.Join(" and ", conditions)));
        }

        return GetODataAsync<RentalSummaryDto>("odata/Rentals?" + string.Join("&", query));
    }

    /// <summary>Danh sách đơn thuê của khách hàng đang đăng nhập.</summary>
    public async Task<ApiResult<List<RentalDto>>> GetMyRentalsAsync()
    {
        var result = await GetAsync<List<RentalDto>>("api/rentals/my");
        result.Data?.ForEach(ResolveDetailImages);
        return result;
    }

    /// <summary>Chi tiết 1 đơn thuê theo Id.</summary>
    public async Task<ApiResult<RentalDto>> GetByIdAsync(int id)
    {
        var result = await GetAsync<RentalDto>($"api/rentals/{id}");
        if (result.Data != null) ResolveDetailImages(result.Data);
        return result;
    }

    /// <summary>Tạo mới đơn thuê (Customer hoặc Staff đặt hộ).</summary>
    public async Task<ApiResult<RentalDto>> CreateAsync(CreateRentalDto dto)
    {
        var result = await PostAsync<RentalDto>("api/rentals", dto);
        if (result.Data != null) ResolveDetailImages(result.Data);
        return result;
    }

    /// <summary>Staff duyệt đơn & xác nhận tiền cọc.</summary>
    public async Task<ApiResult<RentalDto>> ConfirmAsync(int id, ConfirmRentalDto dto)
    {
        var result = await PutAsync<RentalDto>($"api/rentals/{id}/confirm", dto);
        if (result.Data != null) ResolveDetailImages(result.Data);
        return result;
    }

    /// <summary>Staff bàn giao xe cho khách.</summary>
    public async Task<ApiResult<RentalDto>> PickupAsync(int id, PickupRentalDto dto)
    {
        var result = await PutAsync<RentalDto>($"api/rentals/{id}/pickup", dto);
        if (result.Data != null) ResolveDetailImages(result.Data);
        return result;
    }

    /// <summary>Staff nhận trả xe & quyết toán đơn thuê.</summary>
    public async Task<ApiResult<RentalDto>> ReturnAsync(int id, ReturnRentalDto dto)
    {
        var result = await PutAsync<RentalDto>($"api/rentals/{id}/return", dto);
        if (result.Data != null) ResolveDetailImages(result.Data);
        return result;
    }

    /// <summary>Huỷ đơn thuê (Customer hoặc Staff).</summary>
    public async Task<ApiResult<RentalDto>> CancelAsync(int id, CancelRentalDto dto)
    {
        var result = await PutAsync<RentalDto>($"api/rentals/{id}/cancel", dto);
        if (result.Data != null) ResolveDetailImages(result.Data);
        return result;
    }

    /// <summary>Lấy danh sách xe đang Available tại cùng chi nhánh (để khách chọn gộp nhiều xe nếu muốn).</summary>
    public async Task<List<MotorbikeDto>> GetBranchAvailableBikesAsync(int branchId)
    {
        var filter = $"BranchId eq {branchId} and Status eq 'Available'";
        var url = $"odata/Motorbikes?$filter={Uri.EscapeDataString(filter)}&$orderby=ModelName&$top=30";
        var result = await GetODataAsync<MotorbikeDto>(url);
        var items = result.Data?.Items ?? new List<MotorbikeDto>();
        items.ForEach(b => b.ImageUrl = ResolveImageUrl(b.ImageUrl));
        return items;
    }

    private void ResolveDetailImages(RentalDto rental)
    {
        foreach (var d in rental.Details)
        {
            d.ImageUrl = ResolveImageUrl(d.ImageUrl);
        }
    }
}