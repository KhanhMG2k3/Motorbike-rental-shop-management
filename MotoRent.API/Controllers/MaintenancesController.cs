using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.API.Extensions;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Maintenances;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

/// <summary>
/// Quản lý phiếu bảo dưỡng xe (Admin / Staff).
/// Luồng: POST (Scheduled) → PUT start (InProgress, xe → Maintenance) → PUT complete (Completed, xe → Available)
/// </summary>
[ApiController]
[Route("api/maintenances")]
[Authorize(Roles = AppRoles.AdminOrStaff)]
public class MaintenancesController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenancesController(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    /// <summary>Danh sách phiếu, lọc theo xe / chi nhánh / trạng thái.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MaintenanceDto>>>> GetList([FromQuery] MaintenanceQuery query)
        => Ok(ApiResponse<List<MaintenanceDto>>.Ok(await _maintenanceService.GetListAsync(query)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> GetById(int id)
        => Ok(ApiResponse<MaintenanceDto>.Ok(await _maintenanceService.GetByIdAsync(id)));

    /// <summary>Lên lịch bảo dưỡng (trạng thái Scheduled).</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Create(CreateMaintenanceDto dto)
    {
        var created = await _maintenanceService.CreateAsync(dto, User.GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = created.MaintenanceId },
            ApiResponse<MaintenanceDto>.Ok(created, "Đã lên lịch bảo dưỡng."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Update(int id, UpdateMaintenanceDto dto)
        => Ok(ApiResponse<MaintenanceDto>.Ok(await _maintenanceService.UpdateAsync(id, dto), "Cập nhật phiếu bảo dưỡng thành công."));

    /// <summary>Bắt đầu bảo dưỡng: phiếu → InProgress, xe → Maintenance.</summary>
    [HttpPut("{id:int}/start")]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Start(int id)
        => Ok(ApiResponse<MaintenanceDto>.Ok(await _maintenanceService.StartAsync(id), "Đã bắt đầu bảo dưỡng, xe tạm ngừng cho thuê."));

    /// <summary>Hoàn thành: ghi chi phí thực tế, xe → Available.</summary>
    [HttpPut("{id:int}/complete")]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Complete(int id, CompleteMaintenanceDto dto)
        => Ok(ApiResponse<MaintenanceDto>.Ok(await _maintenanceService.CompleteAsync(id, dto), "Đã hoàn thành bảo dưỡng, xe sẵn sàng cho thuê."));

    /// <summary>Huỷ phiếu. Nếu đang bảo dưỡng thì xe về Available.</summary>
    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<MaintenanceDto>>> Cancel(int id)
        => Ok(ApiResponse<MaintenanceDto>.Ok(await _maintenanceService.CancelAsync(id), "Đã huỷ phiếu bảo dưỡng."));
}
