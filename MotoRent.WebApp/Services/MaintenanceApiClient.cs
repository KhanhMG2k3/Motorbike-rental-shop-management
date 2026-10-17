using MotoRent.BusinessObjects.DTOs.Maintenances;

namespace MotoRent.WebApp.Services;

/// <summary>Gọi API phiếu bảo dưỡng (Admin / Staff).</summary>
public class MaintenanceApiClient : ApiClientBase
{
    public MaintenanceApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<List<MaintenanceDto>>> GetListAsync(MaintenanceQuery query)
    {
        var parameters = new List<string>();
        if (query.MotorbikeId.HasValue) parameters.Add($"motorbikeId={query.MotorbikeId}");
        if (query.BranchId.HasValue) parameters.Add($"branchId={query.BranchId}");
        if (!string.IsNullOrEmpty(query.Status)) parameters.Add($"status={Uri.EscapeDataString(query.Status)}");

        var url = "api/maintenances" + (parameters.Count > 0 ? "?" + string.Join("&", parameters) : "");
        return GetAsync<List<MaintenanceDto>>(url);
    }

    public Task<ApiResult<MaintenanceDto>> GetAsync(int id) => GetAsync<MaintenanceDto>($"api/maintenances/{id}");

    public Task<ApiResult<MaintenanceDto>> CreateAsync(CreateMaintenanceDto dto) => PostAsync<MaintenanceDto>("api/maintenances", dto);

    public Task<ApiResult<MaintenanceDto>> UpdateAsync(int id, UpdateMaintenanceDto dto) => PutAsync<MaintenanceDto>($"api/maintenances/{id}", dto);

    public Task<ApiResult<MaintenanceDto>> StartAsync(int id) => PutAsync<MaintenanceDto>($"api/maintenances/{id}/start");

    public Task<ApiResult<MaintenanceDto>> CompleteAsync(int id, CompleteMaintenanceDto dto) => PutAsync<MaintenanceDto>($"api/maintenances/{id}/complete", dto);

    public Task<ApiResult<MaintenanceDto>> CancelAsync(int id) => PutAsync<MaintenanceDto>($"api/maintenances/{id}/cancel");
}
