using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Maintenances;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

/// <summary>
/// Quản lý phiếu bảo dưỡng xe.
/// Vòng đời: Scheduled → (Start) InProgress → (Complete) Completed
///           Scheduled / InProgress → (Cancel) Cancelled
/// Khi InProgress: xe chuyển sang Maintenance (không cho thuê). Khi xong / huỷ: xe về Available.
/// </summary>
public class MaintenanceService : IMaintenanceService
{
    private readonly IGenericRepository<MaintenanceRecord> _maintenanceRepo;
    private readonly IMotorbikeRepository _motorbikeRepo;

    public MaintenanceService(
        IGenericRepository<MaintenanceRecord> maintenanceRepo,
        IMotorbikeRepository motorbikeRepo)
    {
        _maintenanceRepo = maintenanceRepo;
        _motorbikeRepo = motorbikeRepo;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public Task<List<MaintenanceDto>> GetListAsync(MaintenanceQuery query)
    {
        var q = _maintenanceRepo.Query();

        if (query.MotorbikeId.HasValue) q = q.Where(m => m.MotorbikeId == query.MotorbikeId);
        if (query.BranchId.HasValue) q = q.Where(m => m.Motorbike.BranchId == query.BranchId);
        if (!string.IsNullOrEmpty(query.Status)) q = q.Where(m => m.Status == query.Status);

        return q.OrderByDescending(m => m.StartDate)
            .ThenByDescending(m => m.MaintenanceId)
            .Select(MaintenanceMappings.ToDto)
            .ToListAsync();
    }

    public async Task<MaintenanceDto> GetByIdAsync(int id)
    {
        return await _maintenanceRepo.Query()
            .Where(m => m.MaintenanceId == id)
            .Select(MaintenanceMappings.ToDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy phiếu bảo dưỡng có Id = {id}.");
    }

    public async Task<MaintenanceDto> CreateAsync(CreateMaintenanceDto dto, int? createdBy)
    {
        var bike = await _motorbikeRepo.GetByIdAsync(dto.MotorbikeId)
            ?? throw new BusinessException("Xe không tồn tại.");

        if (bike.Status == MotorbikeStatus.Inactive)
            throw new BusinessException("Xe đã ngừng cho thuê, không cần lên lịch bảo dưỡng.");

        await EnsureNoOtherOpenMaintenanceAsync(dto.MotorbikeId);
        await EnsureNoRentalConflictAsync(dto.MotorbikeId, dto.StartDate!.Value, dto.EndDate);

        var entity = new MaintenanceRecord
        {
            MotorbikeId = dto.MotorbikeId,
            Status = MaintenanceStatus.Scheduled,
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now
        };
        dto.ApplyTo(entity);

        await _maintenanceRepo.AddAsync(entity);
        await _maintenanceRepo.SaveChangesAsync();

        return await GetByIdAsync(entity.MaintenanceId);
    }

    public async Task<MaintenanceDto> UpdateAsync(int id, UpdateMaintenanceDto dto)
    {
        var entity = await FindAsync(id);

        if (!MaintenanceStatus.Open.Contains(entity.Status))
            throw new BusinessException("Chỉ sửa được phiếu đang chờ hoặc đang bảo dưỡng.");

        // Phiếu chưa bắt đầu: đổi ngày thì kiểm tra lại lịch thuê
        if (entity.Status == MaintenanceStatus.Scheduled)
            await EnsureNoRentalConflictAsync(entity.MotorbikeId, dto.StartDate!.Value, dto.EndDate);

        dto.ApplyTo(entity);
        await _maintenanceRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<MaintenanceDto> StartAsync(int id)
    {
        var entity = await FindAsync(id);

        if (entity.Status != MaintenanceStatus.Scheduled)
            throw new BusinessException("Chỉ bắt đầu được phiếu đang ở trạng thái chờ (Scheduled).");

        var bike = await _motorbikeRepo.GetByIdAsync(entity.MotorbikeId)
            ?? throw new NotFoundException("Không tìm thấy xe của phiếu bảo dưỡng.");

        if (bike.Status == MotorbikeStatus.Rented)
            throw new BusinessException("Xe đang được khách thuê, chưa thể đưa vào bảo dưỡng.");
        if (bike.Status == MotorbikeStatus.Inactive)
            throw new BusinessException("Xe đã ngừng cho thuê.");

        await EnsureNoRentalConflictAsync(bike.MotorbikeId, Today, entity.EndDate);

        entity.Status = MaintenanceStatus.InProgress;
        if (entity.StartDate > Today) entity.StartDate = Today;

        bike.Status = MotorbikeStatus.Maintenance;
        bike.UpdatedAt = DateTime.Now;

        // Cùng 1 DbContext → 1 lần SaveChanges lưu cả phiếu và xe (atomic)
        await _maintenanceRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<MaintenanceDto> CompleteAsync(int id, CompleteMaintenanceDto dto)
    {
        var entity = await FindAsync(id);

        if (entity.Status != MaintenanceStatus.InProgress)
            throw new BusinessException("Chỉ hoàn thành được phiếu đang bảo dưỡng (InProgress).");

        var endDate = dto.EndDate ?? Today;
        if (endDate < entity.StartDate)
            throw new BusinessException("Ngày hoàn thành không được trước ngày bắt đầu.");
        if (endDate > Today)
            throw new BusinessException("Ngày hoàn thành không được ở tương lai.");

        entity.Status = MaintenanceStatus.Completed;
        entity.EndDate = endDate;
        entity.Cost = dto.Cost;

        await ReleaseMotorbikeAsync(entity.MotorbikeId);
        await _maintenanceRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<MaintenanceDto> CancelAsync(int id)
    {
        var entity = await FindAsync(id);

        if (!MaintenanceStatus.Open.Contains(entity.Status))
            throw new BusinessException("Phiếu đã hoàn thành hoặc đã huỷ.");

        if (entity.Status == MaintenanceStatus.InProgress)
            await ReleaseMotorbikeAsync(entity.MotorbikeId);

        entity.Status = MaintenanceStatus.Cancelled;
        await _maintenanceRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    private async Task<MaintenanceRecord> FindAsync(int id)
        => await _maintenanceRepo.GetByIdAsync(id)
           ?? throw new NotFoundException($"Không tìm thấy phiếu bảo dưỡng có Id = {id}.");

    /// <summary>Bảo dưỡng xong → xe về Available (nếu đang ở Maintenance).</summary>
    private async Task ReleaseMotorbikeAsync(int motorbikeId)
    {
        var bike = await _motorbikeRepo.GetByIdAsync(motorbikeId);
        if (bike is { Status: MotorbikeStatus.Maintenance })
        {
            bike.Status = MotorbikeStatus.Available;
            bike.UpdatedAt = DateTime.Now;
        }
    }

    private async Task EnsureNoOtherOpenMaintenanceAsync(int motorbikeId)
    {
        var hasOpen = await _maintenanceRepo.Query()
            .AnyAsync(m => m.MotorbikeId == motorbikeId && MaintenanceStatus.Open.Contains(m.Status));
        if (hasOpen)
            throw new ConflictException("Xe đã có phiếu bảo dưỡng chưa hoàn thành.");
    }

    /// <summary>Không cho bảo dưỡng trùng thời gian xe đã có khách đặt / đang thuê.</summary>
    private async Task EnsureNoRentalConflictAsync(int motorbikeId, DateOnly start, DateOnly? end)
    {
        var from = start.ToDateTime(TimeOnly.MinValue);
        var to = (end ?? start).AddDays(1).ToDateTime(TimeOnly.MinValue);

        if (await _motorbikeRepo.HasActiveRentalBetweenAsync(motorbikeId, from, to))
            throw new BusinessException("Xe đã có đơn thuê trong khoảng thời gian bảo dưỡng. Hãy chọn ngày khác hoặc xử lý đơn thuê trước.");
    }
}
