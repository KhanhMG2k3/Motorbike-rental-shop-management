using MotoRent.BusinessObjects.DTOs.Branches;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi API quản trị Chi nhánh (Admin).
/// </summary>
public class BranchApiClient : ApiClientBase
{
    public BranchApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<List<BranchDto>>> GetBranchesAsync(bool includeInactive = true)
        => GetAsync<List<BranchDto>>($"api/branches?includeInactive={includeInactive}");

    public Task<ApiResult<BranchDto>> GetBranchAsync(int id)
        => GetAsync<BranchDto>($"api/branches/{id}");

    public Task<ApiResult<BranchDto>> CreateBranchAsync(CreateBranchDto dto)
        => PostAsync<BranchDto>("api/branches", dto);

    public Task<ApiResult<BranchDto>> UpdateBranchAsync(int id, UpdateBranchDto dto)
        => PutAsync<BranchDto>($"api/branches/{id}", dto);

    public Task<ApiResult<object>> DeactivateBranchAsync(int id)
        => DeleteAsync<object>($"api/branches/{id}");

    public Task<ApiResult<object>> ToggleStatusAsync(int id)
        => PatchAsync<object>($"api/branches/{id}/status", new { });
}
