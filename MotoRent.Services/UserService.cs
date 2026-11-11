using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Users;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

public class UserService : IUserService
{
    private readonly IGenericRepository<User> _userRepo;
    private readonly IGenericRepository<Branch> _branchRepo;

    public UserService(IGenericRepository<User> userRepo, IGenericRepository<Branch> branchRepo)
    {
        _userRepo = userRepo;
        _branchRepo = branchRepo;
    }

    public async Task<List<UserDto>> GetUsersAsync(int? roleId = null, int? branchId = null, string? keyword = null)
    {
        var query = _userRepo.Query().AsQueryable();

        if (roleId.HasValue)
            query = query.Where(u => u.RoleId == roleId.Value);

        if (branchId.HasValue)
            query = query.Where(u => u.BranchId == branchId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(kw)
                                  || u.Email.ToLower().Contains(kw)
                                  || (u.Phone != null && u.Phone.Contains(kw)));
        }

        return await query
            .OrderByDescending(u => u.UserId)
            .Select(UserMappings.UserToDto)
            .ToListAsync();
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        return await _userRepo.Query()
            .Where(u => u.UserId == id)
            .Select(UserMappings.UserToDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy người dùng có Id = {id}.");
    }

    public async Task<UserDto> CreateStaffAsync(CreateStaffDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _userRepo.Query().AnyAsync(u => u.Email == email))
            throw new ConflictException("Email này đã được sử dụng.");

        var phone = dto.Phone.Trim();
        if (await _userRepo.Query().AnyAsync(u => u.Phone == phone))
            throw new ConflictException("Số điện thoại này đã được sử dụng.");

        if (!string.IsNullOrWhiteSpace(dto.IdentityNumber))
        {
            var idNumber = dto.IdentityNumber.Trim();
            if (await _userRepo.Query().AnyAsync(u => u.IdentityNumber == idNumber))
                throw new ConflictException("Số CCCD này đã được sử dụng.");
        }

        var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
        if (branch == null || !branch.IsActive)
            throw new BusinessException("Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");

        var staff = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = phone,
            BranchId = dto.BranchId,
            RoleId = 2, // Staff
            DateOfBirth = dto.DateOfBirth,
            Address = dto.Address?.Trim(),
            IdentityNumber = dto.IdentityNumber?.Trim(),
            DriverLicenseNumber = dto.DriverLicenseNumber?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _userRepo.AddAsync(staff);
        await _userRepo.SaveChangesAsync();

        return await GetByIdAsync(staff.UserId);
    }

    public async Task ToggleStatusAsync(int id, int currentAdminId)
    {
        if (id == currentAdminId)
            throw new BusinessException("Không thể tự khoá tài khoản của chính mình.");

        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy người dùng có Id = {id}.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.Now;
        await _userRepo.SaveChangesAsync();
    }
}
