using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers.OData;

/// <summary>
/// Tìm kiếm xe bằng OData (Public). Ví dụ:
///   /odata/Motorbikes?$filter=Status eq 'Available' and PricePerDay le 200000&amp;$orderby=PricePerDay&amp;$top=12&amp;$count=true
///   /odata/Motorbikes?$filter=contains(tolower(ModelName),'vision')
///   /odata/Motorbikes?$filter=BrandName eq 'Honda' and CategoryName eq 'Xe tay ga'
/// </summary>
public class MotorbikesODataController : ODataController
{
    private readonly IMotorbikeService _motorbikeService;

    public MotorbikesODataController(IMotorbikeService motorbikeService)
    {
        _motorbikeService = motorbikeService;
    }

    [HttpGet("odata/Motorbikes")]
    [EnableQuery(MaxTop = 50, PageSize = 50, MaxNodeCount = 200)]
    public IQueryable<MotorbikeDto> Get() => _motorbikeService.QueryForOData();
}
