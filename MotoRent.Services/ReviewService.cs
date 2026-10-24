using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Reviews;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IGenericRepository<Review> _reviewRepo;
        private readonly IGenericRepository<Rental> _rentalRepo;
        private readonly IMotorbikeRepository _motorbikeRepo;
        public ReviewService(
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<Rental> rentalRepo,
            IMotorbikeRepository motorbikeRepo)
        {
            _reviewRepo = reviewRepo;
            _rentalRepo = rentalRepo;
            _motorbikeRepo = motorbikeRepo;
        }
        public async Task<List<ReviewDto>> GetReviewsByMotorbikeIdAsync(int motorbikeId)
        {
            return await _reviewRepo.Query()
                .Where(r => r.MotorbikeId == motorbikeId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(ReviewMappings.ReviewToDto)
                .ToListAsync();
        }
        public async Task<List<ReviewDto>> GetReviewsByRentalIdAsync(int rentalId, int? currentUserId = null, string? currentRole = null)
        {
            var rental = await _rentalRepo.Query().FirstOrDefaultAsync(r => r.RentalId == rentalId)
                ?? throw new NotFoundException($"Không tìm thấy đơn thuê có Id = {rentalId}.");
            if (currentRole == AppRoles.Customer && currentUserId.HasValue && rental.CustomerId != currentUserId.Value)
            {
                throw new BusinessException("Bạn không có quyền xem đánh giá của đơn thuê này.");
            }
            return await _reviewRepo.Query()
                .Where(r => r.RentalId == rentalId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(ReviewMappings.ReviewToDto)
                .ToListAsync();
        }
        public async Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, int customerId)
        {
            // 1. Kiểm tra đơn thuê tồn tại và nạp chi tiết các xe
            var rental = await _rentalRepo.Query()
                .Include(r => r.RentalDetails)
                .FirstOrDefaultAsync(r => r.RentalId == dto.RentalId)
                ?? throw new NotFoundException($"Không tìm thấy đơn thuê có Id = {dto.RentalId}.");
            // 2. BR1: Khách hàng chỉ được đánh giá đơn của chính mình
            if (rental.CustomerId != customerId)
            {
                throw new BusinessException("Bạn chỉ có thể đánh giá cho đơn thuê của chính mình.");
            }
            // 3. BR2: Đơn thuê bắt buộc phải ở trạng thái Completed
            if (rental.Status != RentalStatus.Completed)
            {
                throw new BusinessException($"Chỉ được đánh giá khi đơn thuê đã hoàn thành ('{RentalStatus.Completed}'). Đơn hiện tại đang ở trạng thái '{rental.Status}'.");
            }
            // 4. BR3: Xe được đánh giá phải nằm trong đơn thuê
            bool motorbikeInRental = rental.RentalDetails.Any(d => d.MotorbikeId == dto.MotorbikeId);
            if (!motorbikeInRental)
            {
                throw new BusinessException($"Xe có Id = {dto.MotorbikeId} không nằm trong danh sách xe của đơn thuê này.");
            }
            // 5. BR4: Mỗi xe trong 1 đơn chỉ được đánh giá 1 lần (Check Unique)
            bool hasReviewed = await _reviewRepo.Query()
                .AnyAsync(r => r.RentalId == dto.RentalId && r.MotorbikeId == dto.MotorbikeId);
            if (hasReviewed)
            {
                throw new ConflictException("Bạn đã đánh giá cho xe này trong đơn thuê rồi. Không thể đánh giá thêm lần nữa.");
            }
            // 6. Tạo entity Review
            var review = new Review
            {
                RentalId = dto.RentalId,
                MotorbikeId = dto.MotorbikeId,
                CustomerId = customerId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                CreatedAt = DateTime.Now
            };
            await _reviewRepo.AddAsync(review);
            await _reviewRepo.SaveChangesAsync();
            // 7. Lấy lại review kèm quan hệ để trả về DTO đầy đủ
            var created = await _reviewRepo.Query()
                .Include(r => r.Customer)
                .Include(r => r.Motorbike)
                .Include(r => r.Rental)
                .FirstAsync(r => r.ReviewId == review.ReviewId);
            return created.ToDto();
        }
        public async Task<ReviewEligibilityDto> CheckEligibilityAsync(int rentalId, int motorbikeId, int customerId)
        {
            var rental = await _rentalRepo.Query()
                .Include(r => r.RentalDetails)
                .FirstOrDefaultAsync(r => r.RentalId == rentalId);
            if (rental == null)
            {
                return new ReviewEligibilityDto { CanReview = false, Reason = "Không tìm thấy đơn thuê." };
            }
            if (rental.CustomerId != customerId)
            {
                return new ReviewEligibilityDto { CanReview = false, Reason = "Đơn thuê không thuộc về bạn." };
            }
            if (rental.Status != RentalStatus.Completed)
            {
                return new ReviewEligibilityDto { CanReview = false, Reason = "Đơn thuê chưa hoàn thành." };
            }
            if (!rental.RentalDetails.Any(d => d.MotorbikeId == motorbikeId))
            {
                return new ReviewEligibilityDto { CanReview = false, Reason = "Xe này không có trong đơn thuê." };
            }
            var existing = await _reviewRepo.Query()
                .Include(r => r.Customer)
                .Include(r => r.Motorbike)
                .Include(r => r.Rental)
                .FirstOrDefaultAsync(r => r.RentalId == rentalId && r.MotorbikeId == motorbikeId);
            if (existing != null)
            {
                return new ReviewEligibilityDto
                {
                    CanReview = false,
                    HasReviewed = true,
                    Reason = "Bạn đã đánh giá xe này rồi.",
                    ExistingReview = existing.ToDto()
                };
            }
            return new ReviewEligibilityDto
            {
                CanReview = true,
                HasReviewed = false
            };
        }
    }
}
