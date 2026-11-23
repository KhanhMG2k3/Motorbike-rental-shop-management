using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.DTOs.Branches;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

/// <summary>
/// Dữ liệu dropdown chỉ đọc (Public).
/// CRUD chi nhánh đầy đủ nằm ở BranchesController (Dev 1).
/// </summary>
[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    /// <summary>Các chi nhánh đang hoạt động.</summary>
    [HttpGet("branches")]
    public async Task<ActionResult<ApiResponse<List<BranchLookupDto>>>> GetBranches()
        => Ok(ApiResponse<List<BranchLookupDto>>.Ok(await _lookupService.GetBranchesAsync()));
}
