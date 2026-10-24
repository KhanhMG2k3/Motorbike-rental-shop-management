using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

public class MotorbikeService : IMotorbikeService
{
    private readonly IMotorbikeRepository _motorbikeRepo;
    private readonly IGenericRepository<Brand> _brandRepo;
    private readonly IGenericRepository<Category> _categoryRepo;
    private readonly IGenericRepository<Branch> _branchRepo;
    private readonly IGenericRepository<MaintenanceRecord> _maintenanceRepo;

    public MotorbikeService(
        IMotorbikeRepository motorbikeRepo,
        IGenericRepository<Brand> brandRepo,
        IGenericRepository<Category> categoryRepo,
        IGenericRepository<Branch> branchRepo,
        IGenericRepository<MaintenanceRecord> maintenanceRepo)
    {
        _motorbikeRepo = motorbikeRepo;
        _brandRepo = brandRepo;
        _categoryRepo = categoryRepo;
        _branchRepo = branchRepo;
        _maintenanceRepo = maintenanceRepo;
    }

    public IQueryable<MotorbikeDto> QueryForOData()
        => _motorbikeRepo.Query().Select(CatalogMappings.MotorbikeToDto);

    public async Task<MotorbikeDto> GetByIdAsync(int id)
    {
        return await _motorbikeRepo.Query()
            .Where(m => m.MotorbikeId == id)
            .Select(CatalogMappings.MotorbikeToDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy xe có Id = {id}.");
    }

    public Task<List<MotorbikeDto>> GetAvailableAsync(AvailableMotorbikeQuery query)
    {
        var q = _motorbikeRepo.QueryAvailable(query.StartDate!.Value, query.EndDate!.Value);

        if (query.BranchId.HasValue) q = q.Where(m => m.BranchId == query.BranchId);
        if (query.CategoryId.HasValue) q = q.Where(m => m.CategoryId == query.CategoryId);
        if (query.BrandId.HasValue) q = q.Where(m => m.BrandId == query.BrandId);

        return q.OrderBy(m => m.PricePerDay)
            .Select(CatalogMappings.MotorbikeToDto)
            .ToListAsync();
    }

    public async Task<MotorbikeDto> CreateAsync(CreateMotorbikeDto dto)
    {
        await ValidateReferencesAsync(dto);
        await EnsurePlateUniqueAsync(dto.LicensePlate);

        var entity = dto.ToEntity(MotorbikeStatus.Available);
        await _motorbikeRepo.AddAsync(entity);
        await _motorbikeRepo.SaveChangesAsync();

        return await GetByIdAsync(entity.MotorbikeId);
    }

    public async Task<MotorbikeDto> UpdateAsync(int id, UpdateMotorbikeDto dto)
    {
        var entity = await _motorbikeRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy xe có Id = {id}.");

        await ValidateReferencesAsync(dto);
        await EnsurePlateUniqueAsync(dto.LicensePlate, id);

        if (dto.Odometer < entity.Odometer)
            throw new BusinessException($"Số km không được nhỏ hơn số km hiện tại ({entity.Odometer:N0} km).");

        if (dto.BranchId != entity.BranchId && entity.Status == MotorbikeStatus.Rented)
            throw new BusinessException("Không thể chuyển chi nhánh khi xe đang được thuê.");

        dto.ApplyTo(entity);
        await _motorbikeRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<MotorbikeDto> UpdateStatusAsync(int id, UpdateMotorbikeStatusDto dto)
    {
        var entity = await _motorbikeRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy xe có Id = {id}.");

        if (entity.Status == MotorbikeStatus.Rented)
            throw new BusinessException("Xe đang được thuê, chỉ có thể đổi trạng thái khi khách trả xe.");

        // Ngừng hoạt động / bảo dưỡng khi xe còn đơn đặt trước → phải xử lý đơn trước
        if (dto.Status != MotorbikeStatus.Available && await _motorbikeRepo.HasActiveRentalAsync(id))
            throw new BusinessException("Xe đang có đơn thuê chờ xử lý, hãy huỷ hoặc đổi xe cho các đơn đó trước.");

        // Xe đang có phiếu bảo dưỡng chưa xong → phải hoàn thành / huỷ phiếu, không đổi tay
        if (dto.Status != MotorbikeStatus.Maintenance &&
            await _maintenanceRepo.Query().AnyAsync(x => x.MotorbikeId == id && x.Status == MaintenanceStatus.InProgress))
            throw new BusinessException("Xe đang có phiếu bảo dưỡng chưa xong, hãy hoàn thành hoặc huỷ phiếu bảo dưỡng trước.");

        entity.Status = dto.Status;
        entity.UpdatedAt = DateTime.Now;
        await _motorbikeRepo.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<(MotorbikeDto Motorbike, string? OldImageUrl)> UpdateImageAsync(int id, string imageUrl)
    {
        var entity = await _motorbikeRepo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy xe có Id = {id}.");

        var oldImageUrl = entity.ImageUrl;
        entity.ImageUrl = imageUrl;
        entity.UpdatedAt = DateTime.Now;
        await _motorbikeRepo.SaveChangesAsync();

        return (await GetByIdAsync(id), oldImageUrl);
    }

    /// <summary>Xoá mềm: chuyển xe sang Inactive (giữ lịch sử đơn thuê, đánh giá).</summary>
    public Task DeleteAsync(int id)
        => UpdateStatusAsync(id, new UpdateMotorbikeStatusDto { Status = MotorbikeStatus.Inactive });

    private async Task ValidateReferencesAsync(CreateMotorbikeDto dto)
    {
        var errors = new List<string>();

        if (!await _brandRepo.Query().AnyAsync(b => b.BrandId == dto.BrandId && b.IsActive))
            errors.Add("Hãng xe không tồn tại hoặc đã ngừng hoạt động.");
        if (!await _categoryRepo.Query().AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive))
            errors.Add("Loại xe không tồn tại hoặc đã ngừng hoạt động.");
        if (!await _branchRepo.Query().AnyAsync(b => b.BranchId == dto.BranchId && b.IsActive))
            errors.Add("Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");

        if (errors.Count > 0)
            throw new BusinessException(string.Join(" ", errors));
    }

    private async Task EnsurePlateUniqueAsync(string plate, int? excludeId = null)
    {
        var normalized = CatalogMappings.NormalizePlate(plate);
        if (await _motorbikeRepo.LicensePlateExistsAsync(normalized, excludeId))
            throw new ConflictException($"Biển số '{normalized}' đã tồn tại.");
    }
}
