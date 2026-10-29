using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Reviews;
using MotoRent.Services.Interfaces;

namespace MotoRent.API.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }
        /// <summary>
        /// Lấy danh sách đánh giá của 1 xe (Public).
        /// Khớp đúng route: GET /api/motorbikes/{id}/reviews theo tài liệu phân công.
        /// </summary>
        [HttpGet("/api/motorbikes/{motorbikeId:int}/reviews")]
        public async Task<ActionResult<ApiResponse<List<ReviewDto>>>> GetByMotorbikeId(int motorbikeId)
        {
            var reviews = await _reviewService.GetReviewsByMotorbikeIdAsync(motorbikeId);
            return Ok(ApiResponse<List<ReviewDto>>.Ok(reviews));
        }
        /// <summary>
        /// Lấy danh sách đánh giá trong 1 đơn thuê.
        /// </summary>
        [HttpGet("rental/{rentalId:int}")]
        public async Task<ActionResult<ApiResponse<List<ReviewDto>>>> GetByRentalId(int rentalId)
        {
            int? userId = TryGetCurrentUserId();
            string role = GetCurrentUserRole();
            var reviews = await _reviewService.GetReviewsByRentalIdAsync(rentalId, userId, role);
            return Ok(ApiResponse<List<ReviewDto>>.Ok(reviews));
        }
        /// <summary>
        /// Kiểm tra xem khách có đủ điều kiện đánh giá xe trong đơn thuê không (hỗ trợ WebApp).
        /// </summary>
        [HttpGet("check-eligibility")]
        public async Task<ActionResult<ApiResponse<ReviewEligibilityDto>>> CheckEligibility([FromQuery] int rentalId, [FromQuery] int motorbikeId)
        {
            int customerId = GetCurrentUserId();
            var result = await _reviewService.CheckEligibilityAsync(rentalId, motorbikeId, customerId);
            return Ok(ApiResponse<ReviewEligibilityDto>.Ok(result));
        }
        /// <summary>
        /// Khách hàng tạo đánh giá cho xe trong đơn thuê đã Completed.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview([FromBody] CreateReviewDto dto)
        {
            int customerId = GetCurrentUserId();
            var created = await _reviewService.CreateReviewAsync(dto, customerId);
            return CreatedAtAction(nameof(GetByMotorbikeId), new { motorbikeId = created.MotorbikeId },
                ApiResponse<ReviewDto>.Ok(created, "Gửi đánh giá thành công. Cảm ơn bạn đã phản hồi!"));
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
            // Fallback tạm thời nếu JWT chưa cấu hình xong
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
