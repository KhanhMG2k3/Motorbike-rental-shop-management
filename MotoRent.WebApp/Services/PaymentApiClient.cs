using MotoRent.BusinessObjects.DTOs.Payments;

namespace MotoRent.WebApp.Services;

/// <summary>Gọi API thanh toán (cần đăng nhập — token JWT được BearerTokenHandler gắn tự động).</summary>
public class PaymentApiClient : ApiClientBase
{
    public PaymentApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<PaymentListDto>> GetListAsync(PaymentQuery query)
    {
        var p = new List<string> { $"page={query.Page}", $"pageSize={query.PageSize}" };
        if (query.RentalId.HasValue) p.Add($"rentalId={query.RentalId}");
        if (!string.IsNullOrEmpty(query.Type)) p.Add($"type={Uri.EscapeDataString(query.Type)}");
        if (!string.IsNullOrEmpty(query.Method)) p.Add($"method={Uri.EscapeDataString(query.Method)}");
        if (!string.IsNullOrEmpty(query.Status)) p.Add($"status={Uri.EscapeDataString(query.Status)}");
        if (query.From.HasValue) p.Add($"from={query.From.Value:yyyy-MM-dd}");
        if (query.To.HasValue) p.Add($"to={query.To.Value:yyyy-MM-dd}");

        return GetAsync<PaymentListDto>("api/payments?" + string.Join("&", p));
    }

    public Task<ApiResult<List<PaymentDto>>> GetMyAsync() => GetAsync<List<PaymentDto>>("api/payments/my");

    public Task<ApiResult<PaymentDto>> ConfirmAsync(int id, ConfirmPaymentDto dto) => PutAsync<PaymentDto>($"api/payments/{id}/confirm", dto);

    public Task<ApiResult<PaymentDto>> CancelAsync(int id, CancelPaymentDto dto) => PutAsync<PaymentDto>($"api/payments/{id}/cancel", dto);
}
