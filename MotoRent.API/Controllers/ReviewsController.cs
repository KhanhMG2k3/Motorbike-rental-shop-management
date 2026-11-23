using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.API.Extensions;
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
        /// Lấy danh sách đánh giá trong 1 đơn thuê. Cần đăng nhập: Customer chỉ xem đơn của mình, Staff/Admin xem tất cả.
        /// </summary>
        [HttpGet("rental/{rentalId:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<ReviewDto>>>> GetByRentalId(int rentalId)
        {
            var reviews = await _reviewService.GetReviewsByRentalIdAsync(rentalId, User.GetUserId(), GetCurrentUserRole());
            return Ok(ApiResponse<List<ReviewDto>>.Ok(reviews));
        }

        /// <summary>
        /// Kiểm tra xem khách có đủ điều kiện đánh giá xe trong đơn thuê không (hỗ trợ WebApp). Chỉ Customer.
        /// </summary>
        [HttpGet("check-eligibility")]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<ApiResponse<ReviewEligibilityDto>>> CheckEligibility([FromQuery] int rentalId, [FromQuery] int motorbikeId)
        {
            var result = await _reviewService.CheckEligibilityAsync(rentalId, motorbikeId, RequireUserId());
            return Ok(ApiResponse<ReviewEligibilityDto>.Ok(result));
        }

        /// <summary>
        /// Khách hàng tạo đánh giá cho xe trong đơn thuê đã Completed. Chỉ Customer.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Customer)]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview([FromBody] CreateReviewDto dto)
        {
            var created = await _reviewService.CreateReviewAsync(dto, RequireUserId());
            return CreatedAtAction(nameof(GetByMotorbikeId), new { motorbikeId = created.MotorbikeId },
                ApiResponse<ReviewDto>.Ok(created, "Gửi đánh giá thành công. Cảm ơn bạn đã phản hồi!"));
        }

        // ===== Helpers =====

        /// <summary>UserId từ JWT. Không có → 401 (không còn "tạm coi là user 1" như trước).</summary>
        private int RequireUserId()
            => User.GetUserId() ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");

        private string GetCurrentUserRole()
            => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
