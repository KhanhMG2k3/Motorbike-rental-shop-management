using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Branches;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

public class BranchService : IBranchService
{
    private readonly IGenericRepository<Branch> _branchRepo;

    public BranchService(IGenericRepository<Branch> branchRepo)
    {
        _branchRepo = branchRepo;
    }

    public Task<List<BranchDto>> GetAllAsync(bool includeInactive = false)
    {
        return _branchRepo.Query()
            .Where(b => includeInactive || b.IsActive)
            .OrderBy(b => b.BranchName)
            .Select(BranchMappings.BranchToDto)
            .ToListAsync();
    }

    public async Task<BranchDto> GetByIdAsync(int id)
    {
        return await _branchRepo.Query()
            .Where(b => b.BranchId == id)
            .Select(BranchMappings.BranchToDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy chi nhánh có Id = {id}.");
    }

    public async Task<BranchDto> CreateAsync(CreateBranchDto dto)
    {
        await EnsureNameUniqueAsync(dto.BranchName);

        var entity = dto.ToEntity();
        await _branchRepo.AddAsync(entity);
        await _branchRepo.SaveChangesAsync();

        return await GetByIdAsync(entity.BranchId);
    }

    public async Task<BranchDto> UpdateAsync(int id, UpdateBranchDto dto)
    {
        var entity = await _branchRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy chi nhánh có Id = {id}.");

        await EnsureNameUniqueAsync(dto.BranchName, id);

        if (!dto.IsActive && entity.IsActive)
            await EnsureNoActiveResourcesAsync(id);

        dto.ApplyTo(entity);
        await _branchRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _branchRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy chi nhánh có Id = {id}.");

        await EnsureNoActiveResourcesAsync(id);

        entity.IsActive = false;
        await _branchRepo.SaveChangesAsync();
    }

    private async Task EnsureNameUniqueAsync(string name, int? excludeId = null)
    {
        var trimmed = name.Trim();
        var exists = await _branchRepo.Query()
            .AnyAsync(b => b.BranchName == trimmed && b.BranchId != excludeId);
        if (exists)
            throw new ConflictException($"Chi nhánh '{trimmed}' đã tồn tại.");
    }

    private async Task EnsureNoActiveResourcesAsync(int branchId)
    {
        var hasActiveBikes = await _branchRepo.Query()
            .Where(b => b.BranchId == branchId)
            .SelectMany(b => b.Motorbikes)
            .AnyAsync(m => m.Status != MotorbikeStatus.Inactive);

        if (hasActiveBikes)
            throw new BusinessException("Chi nhánh vẫn còn xe đang hoạt động, hãy điều chuyển hoặc ngừng các xe này trước.");

        var hasActiveStaff = await _branchRepo.Query()
            .Where(b => b.BranchId == branchId)
            .SelectMany(b => b.Users)
            .AnyAsync(u => u.RoleId == 2 && u.IsActive);

        if (hasActiveStaff)
            throw new BusinessException("Chi nhánh vẫn còn nhân viên đang làm việc, hãy điều chuyển nhân viên trước khi ngừng hoạt động.");
    }
}
