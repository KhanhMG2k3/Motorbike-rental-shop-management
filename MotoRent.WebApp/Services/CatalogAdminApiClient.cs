using System.Net.Http.Headers;
using MotoRent.BusinessObjects.DTOs.Branches;
using MotoRent.BusinessObjects.DTOs.Brands;
using MotoRent.BusinessObjects.DTOs.Categories;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.WebApp.Areas.Admin.Models;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi API quản trị danh mục: hãng xe, loại xe, xe máy (Admin).
/// </summary>
public class CatalogAdminApiClient : ApiClientBase
{
    public CatalogAdminApiClient(HttpClient http) : base(http) { }

    // ===== Brands =====
    public Task<ApiResult<List<BrandDto>>> GetBrandsAsync(bool includeInactive = true)
        => GetAsync<List<BrandDto>>($"api/brands?includeInactive={includeInactive}");

    public Task<ApiResult<BrandDto>> GetBrandAsync(int id) => GetAsync<BrandDto>($"api/brands/{id}");

    public Task<ApiResult<BrandDto>> CreateBrandAsync(CreateBrandDto dto) => PostAsync<BrandDto>("api/brands", dto);

    public Task<ApiResult<BrandDto>> UpdateBrandAsync(int id, UpdateBrandDto dto) => PutAsync<BrandDto>($"api/brands/{id}", dto);

    public Task<ApiResult<object>> DeactivateBrandAsync(int id) => DeleteAsync<object>($"api/brands/{id}");

    // ===== Categories =====
    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(bool includeInactive = true)
        => GetAsync<List<CategoryDto>>($"api/categories?includeInactive={includeInactive}");

    public Task<ApiResult<CategoryDto>> GetCategoryAsync(int id) => GetAsync<CategoryDto>($"api/categories/{id}");

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(CreateCategoryDto dto) => PostAsync<CategoryDto>("api/categories", dto);

    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryDto dto) => PutAsync<CategoryDto>($"api/categories/{id}", dto);

    public Task<ApiResult<object>> DeactivateCategoryAsync(int id) => DeleteAsync<object>($"api/categories/{id}");

    // ===== Branches (dropdown) =====
    public Task<ApiResult<List<BranchLookupDto>>> GetBranchesAsync() => GetAsync<List<BranchLookupDto>>("api/lookups/branches");

    // ===== Motorbikes =====

    /// <summary>Tìm xe cho trang quản trị (gồm cả xe Inactive) qua OData.</summary>
    public async Task<ApiResult<PagedResult<MotorbikeDto>>> SearchMotorbikesAsync(AdminMotorbikeFilter filter, int pageSize)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower().Replace("'", "''");
            conditions.Add($"(contains(tolower(ModelName),'{kw}') or contains(tolower(LicensePlate),'{kw}'))");
        }
        if (!string.IsNullOrEmpty(filter.Status)) conditions.Add($"Status eq '{filter.Status.Replace("'", "''")}'");
        if (filter.BranchId.HasValue) conditions.Add($"BranchId eq {filter.BranchId}");
        if (filter.BrandId.HasValue) conditions.Add($"BrandId eq {filter.BrandId}");

        var query = new List<string>
        {
            "$orderby=" + Uri.EscapeDataString("MotorbikeId desc"),
            "$top=" + pageSize,
            "$skip=" + (Math.Max(filter.Page, 1) - 1) * pageSize,
            "$count=true"
        };
        if (conditions.Count > 0)
            query.Insert(0, "$filter=" + Uri.EscapeDataString(string.Join(" and ", conditions)));

        var result = await GetODataAsync<MotorbikeDto>("odata/Motorbikes?" + string.Join("&", query));
        result.Data?.Items.ForEach(m => m.ImageUrl = ResolveImageUrl(m.ImageUrl));
        return result;
    }

    /// <summary>Danh sách xe đang hoạt động cho dropdown (tối đa 50 xe — giới hạn MaxTop của OData).</summary>
    public Task<ApiResult<PagedResult<MotorbikeDto>>> GetSelectableMotorbikesAsync()
    {
        var url = "odata/Motorbikes?$filter=" + Uri.EscapeDataString("Status ne 'Inactive'")
                  + "&$orderby=" + Uri.EscapeDataString("ModelName,LicensePlate")
                  + "&$top=50";
        return GetODataAsync<MotorbikeDto>(url);
    }

    public async Task<ApiResult<MotorbikeDto>> GetMotorbikeAsync(int id)
    {
        var result = await GetAsync<MotorbikeDto>($"api/motorbikes/{id}");
        if (result.Data != null) result.Data.ImageUrl = ResolveImageUrl(result.Data.ImageUrl);
        return result;
    }

    public Task<ApiResult<MotorbikeDto>> CreateMotorbikeAsync(CreateMotorbikeDto dto) => PostAsync<MotorbikeDto>("api/motorbikes", dto);

    public Task<ApiResult<MotorbikeDto>> UpdateMotorbikeAsync(int id, UpdateMotorbikeDto dto) => PutAsync<MotorbikeDto>($"api/motorbikes/{id}", dto);

    public Task<ApiResult<MotorbikeDto>> UpdateMotorbikeStatusAsync(int id, string status)
        => PatchAsync<MotorbikeDto>($"api/motorbikes/{id}/status", new UpdateMotorbikeStatusDto { Status = status });

    public Task<ApiResult<object>> DeactivateMotorbikeAsync(int id) => DeleteAsync<object>($"api/motorbikes/{id}");

    /// <summary>Chuyển tiếp file ảnh người dùng chọn sang API (multipart/form-data).</summary>
    public async Task<ApiResult<MotorbikeDto>> UploadMotorbikeImageAsync(int id, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        return await PostContentAsync<MotorbikeDto>($"api/motorbikes/{id}/image", content);
    }
}
