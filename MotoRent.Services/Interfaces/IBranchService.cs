using MotoRent.BusinessObjects.DTOs.Branches;

namespace MotoRent.Services.Interfaces;

public interface IBranchService
{
    Task<List<BranchDto>> GetAllAsync(bool includeInactive = false);
    Task<BranchDto> GetByIdAsync(int id);
    Task<BranchDto> CreateAsync(CreateBranchDto dto);
    Task<BranchDto> UpdateAsync(int id, UpdateBranchDto dto);
    Task DeleteAsync(int id);
    Task<bool> ToggleStatusAsync(int id);
}
