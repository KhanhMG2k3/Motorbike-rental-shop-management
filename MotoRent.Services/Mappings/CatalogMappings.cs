using System.Linq.Expressions;
using MotoRent.BusinessObjects.DTOs.Brands;
using MotoRent.BusinessObjects.DTOs.Categories;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.Services.Mappings;

/// <summary>
/// Map thủ công Entity ↔ DTO (không dùng AutoMapper).
/// Các Expression dùng trong .Select(...) để EF Core dịch thẳng ra SQL — chỉ lấy đúng cột cần.
/// </summary>
public static class CatalogMappings
{
    // ===== Brand =====
    public static readonly Expression<Func<Brand, BrandDto>> BrandToDto = b => new BrandDto
    {
        BrandId = b.BrandId,
        BrandName = b.BrandName,
        Country = b.Country,
        LogoUrl = b.LogoUrl,
        IsActive = b.IsActive,
        MotorbikeCount = b.Motorbikes.Count
    };

    public static Brand ToEntity(this CreateBrandDto dto) => new()
    {
        BrandName = dto.BrandName.Trim(),
        Country = dto.Country?.Trim(),
        LogoUrl = dto.LogoUrl?.Trim(),
        IsActive = true
    };

    public static void ApplyTo(this UpdateBrandDto dto, Brand entity)
    {
        entity.BrandName = dto.BrandName.Trim();
        entity.Country = dto.Country?.Trim();
        entity.LogoUrl = dto.LogoUrl?.Trim();
        entity.IsActive = dto.IsActive;
    }

    // ===== Category =====
    public static readonly Expression<Func<Category, CategoryDto>> CategoryToDto = c => new CategoryDto
    {
        CategoryId = c.CategoryId,
        CategoryName = c.CategoryName,
        Description = c.Description,
        IsActive = c.IsActive,
        MotorbikeCount = c.Motorbikes.Count
    };

    public static Category ToEntity(this CreateCategoryDto dto) => new()
    {
        CategoryName = dto.CategoryName.Trim(),
        Description = dto.Description?.Trim(),
        IsActive = true
    };

    public static void ApplyTo(this UpdateCategoryDto dto, Category entity)
    {
        entity.CategoryName = dto.CategoryName.Trim();
        entity.Description = dto.Description?.Trim();
        entity.IsActive = dto.IsActive;
    }

    // ===== Motorbike =====
    public static readonly Expression<Func<Motorbike, MotorbikeDto>> MotorbikeToDto = m => new MotorbikeDto
    {
        MotorbikeId = m.MotorbikeId,
        LicensePlate = m.LicensePlate,
        ModelName = m.ModelName,
        BrandId = m.BrandId,
        BrandName = m.Brand.BrandName,
        CategoryId = m.CategoryId,
        CategoryName = m.Category.CategoryName,
        BranchId = m.BranchId,
        BranchName = m.Branch.BranchName,
        ManufactureYear = m.ManufactureYear,
        EngineCapacity = m.EngineCapacity,
        Color = m.Color,
        PricePerDay = m.PricePerDay,
        DepositAmount = m.DepositAmount,
        Odometer = m.Odometer,
        Status = m.Status,
        ImageUrl = m.ImageUrl,
        Description = m.Description,
        AverageRating = m.Reviews.Average(r => (double?)r.Rating),
        ReviewCount = m.Reviews.Count
    };

    public static Motorbike ToEntity(this CreateMotorbikeDto dto, string status) => new()
    {
        LicensePlate = NormalizePlate(dto.LicensePlate),
        ModelName = dto.ModelName.Trim(),
        BrandId = dto.BrandId,
        CategoryId = dto.CategoryId,
        BranchId = dto.BranchId,
        ManufactureYear = dto.ManufactureYear,
        EngineCapacity = dto.EngineCapacity,
        Color = dto.Color.Trim(),
        PricePerDay = dto.PricePerDay,
        DepositAmount = dto.DepositAmount,
        Odometer = dto.Odometer,
        Status = status,
        ImageUrl = dto.ImageUrl?.Trim(),
        Description = dto.Description?.Trim()
    };

    public static void ApplyTo(this UpdateMotorbikeDto dto, Motorbike entity)
    {
        entity.LicensePlate = NormalizePlate(dto.LicensePlate);
        entity.ModelName = dto.ModelName.Trim();
        entity.BrandId = dto.BrandId;
        entity.CategoryId = dto.CategoryId;
        entity.BranchId = dto.BranchId;
        entity.ManufactureYear = dto.ManufactureYear;
        entity.EngineCapacity = dto.EngineCapacity;
        entity.Color = dto.Color.Trim();
        entity.PricePerDay = dto.PricePerDay;
        entity.DepositAmount = dto.DepositAmount;
        entity.Odometer = dto.Odometer;
        entity.ImageUrl = dto.ImageUrl?.Trim();
        entity.Description = dto.Description?.Trim();
        entity.UpdatedAt = DateTime.Now;
    }

    public static string NormalizePlate(string plate) => plate.Trim().ToUpperInvariant();

    // ===== Rental (tóm tắt cho OData) =====
    public static readonly Expression<Func<Rental, RentalSummaryDto>> RentalToSummaryDto = r => new RentalSummaryDto
    {
        RentalId = r.RentalId,
        RentalCode = r.RentalCode,
        CustomerId = r.CustomerId,
        CustomerName = r.Customer.FullName,
        CustomerPhone = r.Customer.Phone,
        StaffId = r.StaffId,
        StaffName = r.Staff != null ? r.Staff.FullName : null,
        BranchId = r.BranchId,
        BranchName = r.Branch.BranchName,
        StartDate = r.StartDate,
        ExpectedReturnDate = r.ExpectedReturnDate,
        ActualReturnDate = r.ActualReturnDate,
        Status = r.Status,
        SubTotal = r.SubTotal,
        DiscountAmount = r.DiscountAmount,
        LateFee = r.LateFee,
        DamageFee = r.DamageFee,
        TotalAmount = r.TotalAmount,
        DepositAmount = r.DepositAmount,
        MotorbikeCount = r.RentalDetails.Count,
        CreatedAt = r.CreatedAt
    };
}
