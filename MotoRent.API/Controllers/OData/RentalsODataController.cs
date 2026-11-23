using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers.OData;

/// <summary>
/// Tra cứu đơn thuê bằng OData (Admin / Staff). Ví dụ:
///   /odata/Rentals?$filter=Status eq 'Renting'&amp;$orderby=ExpectedReturnDate
///   /odata/Rentals?$filter=contains(RentalCode,'20261003') or contains(CustomerPhone,'0933')
///   /odata/Rentals?$filter=StartDate ge 2026-09-01T00:00:00Z and BranchId eq 1&amp;$count=true
/// </summary>
[Authorize(Roles = AppRoles.AdminOrStaff)]
public class RentalsODataController : ODataController
{
    private readonly IRentalQueryService _rentalQueryService;

    public RentalsODataController(IRentalQueryService rentalQueryService)
    {
        _rentalQueryService = rentalQueryService;
    }

    [HttpGet("odata/Rentals")]
    [EnableQuery(MaxTop = 100, PageSize = 50, MaxNodeCount = 200)]
    public IQueryable<RentalSummaryDto> Get() => _rentalQueryService.QueryForOData();
}
