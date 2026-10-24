using MotoRent.BusinessObjects.DTOs.Users;

namespace MotoRent.Services.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(int? roleId = null, int? branchId = null, string? keyword = null);
    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> CreateStaffAsync(CreateStaffDto dto);
    Task ToggleStatusAsync(int id, int currentAdminId);
}
