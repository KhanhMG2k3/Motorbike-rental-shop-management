using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.DTOs.Branches;
using MotoRent.BusinessObjects.Entities;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;

namespace MotoRent.Services;

/// <summary>Dữ liệu cho dropdown (chỉ đọc).</summary>
public class LookupService : ILookupService
{
    private readonly IGenericRepository<Branch> _branchRepo;

    public LookupService(IGenericRepository<Branch> branchRepo)
    {
        _branchRepo = branchRepo;
    }

    public Task<List<BranchLookupDto>> GetBranchesAsync()
    {
        return _branchRepo.Query()
            .Where(b => b.IsActive)
            .OrderBy(b => b.BranchName)
            .Select(b => new BranchLookupDto
            {
                BranchId = b.BranchId,
                BranchName = b.BranchName,
                Address = b.Address
            })
            .ToListAsync();
    }
}
