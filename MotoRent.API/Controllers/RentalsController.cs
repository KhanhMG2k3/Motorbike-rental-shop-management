using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers
{
    [Route("api/rentals")]
    [ApiController]
    public class RentalsController : ControllerBase
    {
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
            int? userId = TryGetCurrentUserId();
            string role = GetCurrentUserRole();
            var result = await _rentalService.GetByIdAsync(id, userId, role);
            return Ok(ApiResponse<RentalDto>.Ok(result));
        }
        /// <summary>
        /// Danh sách đơn thuê của khách hàng đang đăng nhập (Customer).
        /// </summary>
        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<List<RentalDto>>>> GetMyRentals()
        {
            int customerId = GetCurrentUserId();
            var list = await _rentalService.GetMyRentalsAsync(customerId);
            return Ok(ApiResponse<List<RentalDto>>.Ok(list));
        }
        /// <summary>
        /// Tạo mới đơn thuê (Customer hoặc Staff).
        /// Kiểm tra trùng lịch, snapshot giá thuê và sinh mã RNT-yyyyMMdd-xxxx.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<RentalDto>>> Create([FromBody] CreateRentalDto dto)
        {
            int userId = GetCurrentUserId();
            string role = GetCurrentUserRole();
            var created = await _rentalService.CreateAsync(dto, userId, role);
            return CreatedAtAction(nameof(GetById), new { id = created.RentalId },
                ApiResponse<RentalDto>.Ok(created, "Tạo đơn thuê thành công. Vui lòng thanh toán cọc để được xác nhận."));
        }
        /// <summary>
        /// Staff duyệt đơn Pending -> Confirmed và ghi nhận tiền cọc (Deposit).
        /// </summary>
        [HttpPut("{id:int}/confirm")]
        public async Task<ActionResult<ApiResponse<RentalDto>>> Confirm(int id, [FromBody] ConfirmRentalDto dto)
        {
            int staffId = GetCurrentUserId();
            var updated = await _rentalService.ConfirmAsync(id, dto, staffId);
            return Ok(ApiResponse<RentalDto>.Ok(updated, "Duyệt đơn và xác nhận tiền cọc thành công."));
        }
        /// <summary>
        /// Staff bàn giao xe cho khách: kiểm tra CCCD + GPLX, ghi số km và đổi trạng thái xe sang Rented.
        /// </summary>
        [HttpPut("{id:int}/pickup")]
        public async Task<ActionResult<ApiResponse<RentalDto>>> Pickup(int id, [FromBody] PickupRentalDto dto)
        {
            int staffId = GetCurrentUserId();
            var updated = await _rentalService.PickupAsync(id, dto, staffId);
            return Ok(ApiResponse<RentalDto>.Ok(updated, "Bàn giao xe thành công. Đơn chuyển sang trạng thái đang thuê."));
        }
        /// <summary>
        /// Staff nhận xe trả: tính số km, phí trễ hạn, hư hỏng, hoàn cọc và cập nhật trạng thái xe.
        /// </summary>
        [HttpPut("{id:int}/return")]
        public async Task<ActionResult<ApiResponse<RentalDto>>> Return(int id, [FromBody] ReturnRentalDto dto)
        {
            int staffId = GetCurrentUserId();
            var updated = await _rentalService.ReturnAsync(id, dto, staffId);
            return Ok(ApiResponse<RentalDto>.Ok(updated, "Nhận trả xe và quyết toán đơn thuê thành công."));
        }
        /// <summary>
        /// Huỷ đơn thuê: Customer chỉ huỷ được đơn của mình; Staff/Admin huỷ bất kỳ đơn nào. Hoàn cọc nếu đã duyệt.
        /// </summary>
        [HttpPut("{id:int}/cancel")]
        public async Task<ActionResult<ApiResponse<RentalDto>>> Cancel(int id, [FromBody] CancelRentalDto dto)
        {
            int userId = GetCurrentUserId();
            string role = GetCurrentUserRole();
            var updated = await _rentalService.CancelAsync(id, dto, userId, role);
            return Ok(ApiResponse<RentalDto>.Ok(updated, "Huỷ đơn thuê thành công."));
        }
        // ===== Helpers =====
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub")
                     ?? User.FindFirst("id")
                     ?? User.FindFirst("userId");
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }
            // Tạm thời fallback = 1 nếu nhóm của bạn chưa hoàn thiện JWT middleware ở tuần 5
            return 1;
        }
        private int? TryGetCurrentUserId()
        {
            try
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
                return claim != null && int.TryParse(claim.Value, out int id) ? id : 1;
            }
            catch
            {
                return null;
            }
        }
        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? AppRoles.Customer;
        }
    }
}
