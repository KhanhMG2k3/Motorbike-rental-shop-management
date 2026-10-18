using System.Linq.Expressions;
using MotoRent.BusinessObjects.DTOs.Auth;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.Services.Mappings;

public static class UserMappings
{
    public static readonly Expression<Func<User, UserProfileDto>> UserToProfileDto = u => new UserProfileDto
    {
        UserId = u.UserId,
        Email = u.Email,
        FullName = u.FullName,
        Phone = u.Phone,
        DateOfBirth = u.DateOfBirth,
        Address = u.Address,
        IdentityNumber = u.IdentityNumber,
        DriverLicenseNumber = u.DriverLicenseNumber,
        RoleId = u.RoleId,
        RoleName = u.Role.RoleName,
        BranchId = u.BranchId,
        BranchName = u.Branch != null ? u.Branch.BranchName : null,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };

    public static void ApplyTo(this UpdateProfileDto dto, User entity)
    {
        entity.FullName = dto.FullName.Trim();
        entity.Phone = dto.Phone?.Trim();
        entity.DateOfBirth = dto.DateOfBirth;
        entity.Address = dto.Address?.Trim();
        entity.IdentityNumber = dto.IdentityNumber?.Trim();
        entity.DriverLicenseNumber = dto.DriverLicenseNumber?.Trim();
        entity.UpdatedAt = DateTime.Now;
    }
}
