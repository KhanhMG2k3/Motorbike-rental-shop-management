using System.Linq.Expressions;
using MotoRent.BusinessObjects.DTOs.Branches;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.Services.Mappings;

public static class BranchMappings
{
    public static readonly Expression<Func<Branch, BranchDto>> BranchToDto = b => new BranchDto
    {
        BranchId = b.BranchId,
        BranchName = b.BranchName,
        Address = b.Address,
        Phone = b.Phone,
        OpenTime = b.OpenTime,
        CloseTime = b.CloseTime,
        IsActive = b.IsActive,
        CreatedAt = b.CreatedAt,
        MotorbikeCount = b.Motorbikes.Count,
        StaffCount = b.Users.Count(u => u.RoleId == 2 && u.IsActive)
    };

    public static Branch ToEntity(this CreateBranchDto dto) => new()
    {
        BranchName = dto.BranchName.Trim(),
        Address = dto.Address.Trim(),
        Phone = dto.Phone.Trim(),
        OpenTime = dto.OpenTime,
        CloseTime = dto.CloseTime,
        IsActive = true,
        CreatedAt = DateTime.Now
    };

    public static void ApplyTo(this UpdateBranchDto dto, Branch entity)
    {
        entity.BranchName = dto.BranchName.Trim();
        entity.Address = dto.Address.Trim();
        entity.Phone = dto.Phone.Trim();
        entity.OpenTime = dto.OpenTime;
        entity.CloseTime = dto.CloseTime;
        entity.IsActive = dto.IsActive;
    }
}
