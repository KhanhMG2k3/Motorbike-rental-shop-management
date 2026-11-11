using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MotoRent.BusinessObjects.DTOs.Reviews;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.Services.Mappings
{
    public static class ReviewMappings
    {
        public static readonly Expression<Func<Review, ReviewDto>> ReviewToDto = r => new ReviewDto
        {
            ReviewId = r.ReviewId,
            RentalId = r.RentalId,
            RentalCode = r.Rental.RentalCode,
            MotorbikeId = r.MotorbikeId,
            ModelName = r.Motorbike.ModelName,
            LicensePlate = r.Motorbike.LicensePlate,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer.FullName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        };
        public static ReviewDto ToDto(this Review r)
        {
            return new ReviewDto
            {
                ReviewId = r.ReviewId,
                RentalId = r.RentalId,
                RentalCode = r.Rental?.RentalCode ?? string.Empty,
                MotorbikeId = r.MotorbikeId,
                ModelName = r.Motorbike?.ModelName ?? string.Empty,
                LicensePlate = r.Motorbike?.LicensePlate ?? string.Empty,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer?.FullName ?? string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            };
        }
    }
}
