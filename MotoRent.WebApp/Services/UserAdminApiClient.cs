using MotoRent.BusinessObjects.DTOs.Users;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi API quản trị người dùng & nhân viên (Admin).
/// </summary>
public class UserAdminApiClient : ApiClientBase
{
    public UserAdminApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<List<UserDto>>> GetUsersAsync(int? roleId = null, int? branchId = null, string? keyword = null)
    {
        var query = new List<string>();
        if (roleId.HasValue) query.Add($"roleId={roleId.Value}");
        if (branchId.HasValue) query.Add($"branchId={branchId.Value}");
        if (!string.IsNullOrWhiteSpace(keyword)) query.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");

        var url = "api/users" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return GetAsync<List<UserDto>>(url);
    }

    public Task<ApiResult<UserDto>> GetUserAsync(int id)
        => GetAsync<UserDto>($"api/users/{id}");

    public Task<ApiResult<UserDto>> CreateStaffAsync(CreateStaffDto dto)
        => PostAsync<UserDto>("api/users/staff", dto);

    public Task<ApiResult<object>> ToggleStatusAsync(int id)
        => PatchAsync<object>($"api/users/{id}/status", new { });
}
