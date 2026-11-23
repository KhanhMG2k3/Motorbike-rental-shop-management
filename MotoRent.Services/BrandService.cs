using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Brands;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

public class BrandService : IBrandService
{
    private readonly IGenericRepository<Brand> _brandRepo;

    public BrandService(IGenericRepository<Brand> brandRepo)
    {
        _brandRepo = brandRepo;
    }

    public Task<List<BrandDto>> GetAllAsync(bool includeInactive)
    {
        return _brandRepo.Query()
            .Where(b => includeInactive || b.IsActive)
            .OrderBy(b => b.BrandName)
            .Select(CatalogMappings.BrandToDto)
            .ToListAsync();
    }

    public async Task<BrandDto> GetByIdAsync(int id)
    {
        return await _brandRepo.Query()
            .Where(b => b.BrandId == id)
            .Select(CatalogMappings.BrandToDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy hãng xe có Id = {id}.");
    }

    public async Task<BrandDto> CreateAsync(CreateBrandDto dto)
    {
        await EnsureNameUniqueAsync(dto.BrandName);

        var entity = dto.ToEntity();
        await _brandRepo.AddAsync(entity);
        await _brandRepo.SaveChangesAsync();

        return await GetByIdAsync(entity.BrandId);
    }

    public async Task<BrandDto> UpdateAsync(int id, UpdateBrandDto dto)
    {
        var entity = await _brandRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy hãng xe có Id = {id}.");

        await EnsureNameUniqueAsync(dto.BrandName, id);
        if (!dto.IsActive && entity.IsActive)
            await EnsureNoActiveMotorbikesAsync(id);

        dto.ApplyTo(entity);
        await _brandRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    /// <summary>Xoá mềm: chỉ đặt IsActive = false để giữ lịch sử xe / đơn thuê.</summary>
    public async Task DeleteAsync(int id)
    {
        var entity = await _brandRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy hãng xe có Id = {id}.");

        await EnsureNoActiveMotorbikesAsync(id);

        entity.IsActive = false;
        await _brandRepo.SaveChangesAsync();
    }

    private async Task EnsureNameUniqueAsync(string name, int? excludeId = null)
    {
        var trimmed = name.Trim();
        var exists = await _brandRepo.Query()
            .AnyAsync(b => b.BrandName == trimmed && b.BrandId != excludeId);
        if (exists)
            throw new ConflictException($"Hãng xe '{trimmed}' đã tồn tại.");
    }

    private async Task EnsureNoActiveMotorbikesAsync(int brandId)
    {
        var hasActive = await _brandRepo.Query()
            .Where(b => b.BrandId == brandId)
            .SelectMany(b => b.Motorbikes)
            .AnyAsync(m => m.Status != MotorbikeStatus.Inactive);
        if (hasActive)
            throw new BusinessException("Hãng xe vẫn còn xe đang hoạt động, hãy ngừng các xe này trước.");
    }
}
