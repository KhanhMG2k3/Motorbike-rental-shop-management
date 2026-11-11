using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.BusinessObjects.Entities;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

/// <summary>
/// Chỉ phục vụ tra cứu đơn thuê qua OData.
/// Nghiệp vụ tạo / duyệt / giao / trả xe thuộc RentalService (Dev 3).
/// </summary>
public class RentalQueryService : IRentalQueryService
{
    private readonly IGenericRepository<Rental> _rentalRepo;

    public RentalQueryService(IGenericRepository<Rental> rentalRepo)
    {
        _rentalRepo = rentalRepo;
    }

    public IQueryable<RentalSummaryDto> QueryForOData()
        => _rentalRepo.Query().Select(CatalogMappings.RentalToSummaryDto);
}
