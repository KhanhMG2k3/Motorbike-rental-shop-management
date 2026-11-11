using MotoRent.BusinessObjects.DTOs.Reviews;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi API Đánh giá xe (/api/reviews & /api/motorbikes/{id}/reviews).
/// </summary>
public class ReviewApiClient : ApiClientBase
{
    public ReviewApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<List<ReviewDto>>> GetByMotorbikeAsync(int motorbikeId)
        => GetAsync<List<ReviewDto>>($"api/motorbikes/{motorbikeId}/reviews");

    public Task<ApiResult<List<ReviewDto>>> GetByRentalAsync(int rentalId)
        => GetAsync<List<ReviewDto>>($"api/reviews/rental/{rentalId}");

    public Task<ApiResult<ReviewDto>> CreateAsync(CreateReviewDto dto)
        => PostAsync<ReviewDto>("api/reviews", dto);
}