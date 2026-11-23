using System.Linq.Expressions;
using MotoRent.BusinessObjects.DTOs.Maintenances;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.Services.Mappings;

public static class MaintenanceMappings
{
    public static readonly Expression<Func<MaintenanceRecord, MaintenanceDto>> ToDto = m => new MaintenanceDto
    {
        MaintenanceId = m.MaintenanceId,
        MotorbikeId = m.MotorbikeId,
        LicensePlate = m.Motorbike.LicensePlate,
        ModelName = m.Motorbike.ModelName,
        MotorbikeStatus = m.Motorbike.Status,
        BranchId = m.Motorbike.BranchId,
        BranchName = m.Motorbike.Branch.BranchName,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Description = m.Description,
        Cost = m.Cost,
        Status = m.Status,
        CreatedBy = m.CreatedBy,
        CreatedByName = m.CreatedByNavigation != null ? m.CreatedByNavigation.FullName : null,
        CreatedAt = m.CreatedAt
    };

    public static void ApplyTo(this UpdateMaintenanceDto dto, MaintenanceRecord entity)
    {
        entity.StartDate = dto.StartDate!.Value;
        entity.EndDate = dto.EndDate;
        entity.Description = dto.Description.Trim();
        entity.Cost = dto.Cost;
    }
}
