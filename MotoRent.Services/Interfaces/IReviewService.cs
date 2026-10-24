using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MotoRent.BusinessObjects.DTOs.Reviews;

namespace MotoRent.Services.Interfaces
{
    public interface IReviewService
    {
        /// <summary>Lấy danh sách đánh giá của 1 xe (Public - hiển thị ở trang chi tiết xe).</summary>
        Task<List<ReviewDto>> GetReviewsByMotorbikeIdAsync(int motorbikeId);
        /// <summary>Lấy các đánh giá trong 1 đơn thuê.</summary>
        Task<List<ReviewDto>> GetReviewsByRentalIdAsync(int rentalId, int? currentUserId = null, string? currentRole = null);
        /// <summary>Khách hàng gửi đánh giá cho 1 xe trong đơn thuê đã Completed.</summary>
        Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, int customerId);
        /// <summary>Kiểm tra điều kiện khách có được đánh giá xe trong đơn thuê không.</summary>
        Task<ReviewEligibilityDto> CheckEligibilityAsync(int rentalId, int motorbikeId, int customerId);
    }
}
