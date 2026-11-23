using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoRent.BusinessObjects.DTOs.Reviews
{
    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int RentalId { get; set; }
        public string RentalCode { get; set; } = null!;
        public int MotorbikeId { get; set; }
        public string ModelName { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public byte Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class CreateReviewDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn đơn thuê hợp lệ.")]
        public int RentalId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn xe cần đánh giá.")]
        public int MotorbikeId { get; set; }
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
        public byte Rating { get; set; }
        [StringLength(1000, ErrorMessage = "Bình luận tối đa 1000 ký tự.")]
        public string? Comment { get; set; }
    }
    /// <summary>
    /// DTO trả về cho WebApp kiểm tra xem khách có đủ điều kiện đánh giá xe này hay không
    /// </summary>
    public class ReviewEligibilityDto
    {
        public bool CanReview { get; set; }
        public string? Reason { get; set; }
        public bool HasReviewed { get; set; }
        public ReviewDto? ExistingReview { get; set; }
    }
}
