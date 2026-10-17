using Microsoft.AspNetCore.Mvc;
using MotoRent.API.Models;
using MotoRent.API.Services;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

/// <summary>
/// CRUD xe máy. Danh sách + tìm kiếm dùng OData: GET /odata/Motorbikes
/// </summary>
[ApiController]
[Route("api/motorbikes")]
public class MotorbikesController : ControllerBase
{
    private readonly IMotorbikeService _motorbikeService;
    private readonly IFileStorageService _fileStorage;

    public MotorbikesController(IMotorbikeService motorbikeService, IFileStorageService fileStorage)
    {
        _motorbikeService = motorbikeService;
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Upload ảnh xe (multipart/form-data, field "file"). Chấp nhận .jpg/.jpeg/.png/.webp, tối đa 2 MB.
    /// Ảnh cũ (nếu là ảnh đã upload) sẽ bị xoá.
    /// </summary>
    // TODO (Dev 1): [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:int}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<MotorbikeDto>>> UploadImage(int id, [FromForm] UploadImageRequest request)
    {
        await _motorbikeService.GetByIdAsync(id); // 404 nếu xe không tồn tại, trước khi ghi file

        var newUrl = await _fileStorage.SaveImageAsync(request.File, "motorbikes");
        try
        {
            var (motorbike, oldUrl) = await _motorbikeService.UpdateImageAsync(id, newUrl);
            _fileStorage.DeleteIfUploaded(oldUrl);
            return Ok(ApiResponse<MotorbikeDto>.Ok(motorbike, "Cập nhật ảnh xe thành công."));
        }
        catch
        {
            _fileStorage.DeleteIfUploaded(newUrl); // lưu DB lỗi → dọn file vừa ghi
            throw;
        }
    }

    /// <summary>Chi tiết xe (Public).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<MotorbikeDto>>> GetById(int id)
        => Ok(ApiResponse<MotorbikeDto>.Ok(await _motorbikeService.GetByIdAsync(id)));

    /// <summary>
    /// Xe còn trống trong khoảng thời gian (Public).
    /// VD: /api/motorbikes/available?startDate=2026-10-20T08:00&amp;endDate=2026-10-22T08:00&amp;branchId=1
    /// </summary>
    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<List<MotorbikeDto>>>> GetAvailable([FromQuery] AvailableMotorbikeQuery query)
        => Ok(ApiResponse<List<MotorbikeDto>>.Ok(await _motorbikeService.GetAvailableAsync(query)));

    // TODO (Dev 1): thêm [Authorize(Roles = AppRoles.Admin)] cho POST / PUT / DELETE khi đã có JWT
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MotorbikeDto>>> Create(CreateMotorbikeDto dto)
    {
        var created = await _motorbikeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.MotorbikeId },
            ApiResponse<MotorbikeDto>.Ok(created, "Thêm xe thành công."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<MotorbikeDto>>> Update(int id, UpdateMotorbikeDto dto)
        => Ok(ApiResponse<MotorbikeDto>.Ok(await _motorbikeService.UpdateAsync(id, dto), "Cập nhật xe thành công."));

    // TODO (Dev 1): [Authorize(Roles = AppRoles.AdminOrStaff)]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<MotorbikeDto>>> UpdateStatus(int id, UpdateMotorbikeStatusDto dto)
        => Ok(ApiResponse<MotorbikeDto>.Ok(await _motorbikeService.UpdateStatusAsync(id, dto), "Cập nhật trạng thái xe thành công."));

    /// <summary>Xoá mềm: chuyển xe sang Inactive.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _motorbikeService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Đã ngừng cho thuê xe."));
    }
}
