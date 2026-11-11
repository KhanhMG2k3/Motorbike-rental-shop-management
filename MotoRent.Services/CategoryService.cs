using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Categories;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

public class CategoryService : ICategoryService
{
    private readonly IGenericRepository<Category> _categoryRepo;

    public CategoryService(IGenericRepository<Category> categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public Task<List<CategoryDto>> GetAllAsync(bool includeInactive)
    {
        return _categoryRepo.Query()
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(CatalogMappings.CategoryToDto)
            .ToListAsync();
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        return await _categoryRepo.Query()
            .Where(c => c.CategoryId == id)
            .Select(CatalogMappings.CategoryToDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy loại xe có Id = {id}.");
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        await EnsureNameUniqueAsync(dto.CategoryName);

        var entity = dto.ToEntity();
        await _categoryRepo.AddAsync(entity);
        await _categoryRepo.SaveChangesAsync();

        return await GetByIdAsync(entity.CategoryId);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var entity = await _categoryRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy loại xe có Id = {id}.");

        await EnsureNameUniqueAsync(dto.CategoryName, id);
        if (!dto.IsActive && entity.IsActive)
            await EnsureNoActiveMotorbikesAsync(id);

        dto.ApplyTo(entity);
        await _categoryRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    /// <summary>Xoá mềm: chỉ đặt IsActive = false.</summary>
    public async Task DeleteAsync(int id)
    {
        var entity = await _categoryRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy loại xe có Id = {id}.");

        await EnsureNoActiveMotorbikesAsync(id);

        entity.IsActive = false;
        await _categoryRepo.SaveChangesAsync();
    }

    private async Task EnsureNameUniqueAsync(string name, int? excludeId = null)
    {
        var trimmed = name.Trim();
        var exists = await _categoryRepo.Query()
            .AnyAsync(c => c.CategoryName == trimmed && c.CategoryId != excludeId);
        if (exists)
            throw new ConflictException($"Loại xe '{trimmed}' đã tồn tại.");
    }

    private async Task EnsureNoActiveMotorbikesAsync(int categoryId)
    {
        var hasActive = await _categoryRepo.Query()
            .Where(c => c.CategoryId == categoryId)
            .SelectMany(c => c.Motorbikes)
            .AnyAsync(m => m.Status != MotorbikeStatus.Inactive);
        if (hasActive)
            throw new BusinessException("Loại xe vẫn còn xe đang hoạt động, hãy ngừng các xe này trước.");
    }
}
