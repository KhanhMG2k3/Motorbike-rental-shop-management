using MotoRent.BusinessObjects.DTOs.Brands;
using MotoRent.BusinessObjects.DTOs.Categories;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.BusinessObjects.DTOs.Rentals;

namespace MotoRent.Services.Interfaces;

public interface IBrandService
{
    Task<List<BrandDto>> GetAllAsync(bool includeInactive);
    Task<BrandDto> GetByIdAsync(int id);
    Task<BrandDto> CreateAsync(CreateBrandDto dto);
    Task<BrandDto> UpdateAsync(int id, UpdateBrandDto dto);
    Task DeleteAsync(int id);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(bool includeInactive);
    Task<CategoryDto> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto);
    Task DeleteAsync(int id);
}

public interface IMotorbikeService
{
    /// <summary>IQueryable cho OData — chưa thực thi, OData sẽ ghép $filter/$orderby/$top rồi mới chạy SQL.</summary>
    IQueryable<MotorbikeDto> QueryForOData();

    Task<MotorbikeDto> GetByIdAsync(int id);
    Task<List<MotorbikeDto>> GetAvailableAsync(AvailableMotorbikeQuery query);
    Task<MotorbikeDto> CreateAsync(CreateMotorbikeDto dto);
    Task<MotorbikeDto> UpdateAsync(int id, UpdateMotorbikeDto dto);
    Task<MotorbikeDto> UpdateStatusAsync(int id, UpdateMotorbikeStatusDto dto);
    Task DeleteAsync(int id);
}

public interface IRentalQueryService
{
    IQueryable<RentalSummaryDto> QueryForOData();
}
