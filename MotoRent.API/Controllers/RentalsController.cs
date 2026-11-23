using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.API.Extensions;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers;

[ApiController]
[Route("api/rentals")]
[Authorize]
public class RentalsController : ControllerBase
{
    private const string CustomerOrStaff = AppRoles.Customer + "," + AppRoles.Staff;

    private readonly IRentalService _rentalService;

    public RentalsController(IRentalService rentalService)
    {
        _rentalService = rentalService;
    }

    /// <summary>
    /// Lấy chi tiết đơn thuê theo Id.
    /// Khách chỉ xem được đơn của mình, Staff/Admin xem được tất cả.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<RentalDto>>> GetById(int id)
    {
        int userId = GetRequiredUserId();
        string role = GetRequiredUserRole();
        var result = await _rentalService.GetByIdAsync(id, userId, role);
        return Ok(ApiResponse<RentalDto>.Ok(result));
    }

    /// <summary>
    /// Danh sách đơn thuê của khách hàng đang đăng nhập (Customer).
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<ApiResponse<List<RentalDto>>>> GetMyRentals()
    {
        int customerId = GetRequiredUserId();
        var list = await _rentalService.GetMyRentalsAsync(customerId);
        return Ok(ApiResponse<List<RentalDto>>.Ok(list));
    }

    /// <summary>
    /// Tạo mới đơn thuê:
    /// - Customer: tự lấy CustomerId từ JWT token (bỏ qua CustomerId trong body).
    /// - Staff: tạo hộ khách, bắt buộc truyền CustomerId hợp lệ trong body.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = CustomerOrStaff)]
    public async Task<ActionResult<ApiResponse<RentalDto>>> Create([FromBody] CreateRentalDto dto)
    {
        int userId = GetRequiredUserId();
        string role = GetRequiredUserRole();

        var created = await _rentalService.CreateAsync(dto, userId, role);
        return CreatedAtAction(nameof(GetById), new { id = created.RentalId },
            ApiResponse<RentalDto>.Ok(created, "Tạo đơn thuê thành công. Vui lòng thanh toán cọc để được xác nhận."));
    }

    /// <summary>
    /// Staff duyệt đơn Pending -> Confirmed và ghi nhận tiền cọc (Deposit).
    /// </summary>
    [HttpPut("{id:int}/confirm")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<ApiResponse<RentalDto>>> Confirm(int id, [FromBody] ConfirmRentalDto dto)
    {
        int staffId = GetRequiredUserId();
        var updated = await _rentalService.ConfirmAsync(id, dto, staffId);
        return Ok(ApiResponse<RentalDto>.Ok(updated, "Duyệt đơn và xác nhận tiền cọc thành công."));
    }

    /// <summary>
    /// Staff bàn giao xe cho khách: kiểm tra CCCD + GPLX, ghi số km và đổi trạng thái xe sang Rented.
    /// </summary>
    [HttpPut("{id:int}/pickup")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<ApiResponse<RentalDto>>> Pickup(int id, [FromBody] PickupRentalDto dto)
    {
        int staffId = GetRequiredUserId();
        var updated = await _rentalService.PickupAsync(id, dto, staffId);
        return Ok(ApiResponse<RentalDto>.Ok(updated, "Bàn giao xe thành công. Đơn chuyển sang trạng thái đang thuê."));
    }

    /// <summary>
    /// Staff nhận xe trả: tính số km, phí trễ hạn, hư hỏng, hoàn cọc và cập nhật trạng thái xe.
    /// </summary>
    [HttpPut("{id:int}/return")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<ApiResponse<RentalDto>>> Return(int id, [FromBody] ReturnRentalDto dto)
    {
        int staffId = GetRequiredUserId();
        var updated = await _rentalService.ReturnAsync(id, dto, staffId);
        return Ok(ApiResponse<RentalDto>.Ok(updated, "Nhận trả xe và quyết toán đơn thuê thành công."));
    }

    /// <summary>
    /// Huỷ đơn thuê: Customer chỉ huỷ được đơn của mình; Staff huỷ bất kỳ đơn nào. Hoàn cọc nếu đã duyệt.
    /// </summary>
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = CustomerOrStaff)]
    public async Task<ActionResult<ApiResponse<RentalDto>>> Cancel(int id, [FromBody] CancelRentalDto dto)
    {
        int userId = GetRequiredUserId();
        string role = GetRequiredUserRole();
        var updated = await _rentalService.CancelAsync(id, dto, userId, role);
        return Ok(ApiResponse<RentalDto>.Ok(updated, "Huỷ đơn thuê thành công."));
    }

    // ===== Helpers =====
    private int GetRequiredUserId()
        => User.GetUserId()
            ?? throw new BusinessException("Không xác định được danh tính người dùng từ token.");

    private string GetRequiredUserRole()
        => User.FindFirstValue(ClaimTypes.Role)
            ?? throw new BusinessException("Không xác định được vai trò người dùng từ token.");
}