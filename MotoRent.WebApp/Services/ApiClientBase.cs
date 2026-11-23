using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MotoRent.BusinessObjects.Common;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Lớp cơ sở cho các API client: gửi request, đọc ApiResponse, bắt lỗi mất kết nối.
/// (Dev 1: gắn JWT bằng DelegatingHandler khi đăng ký HttpClient trong Program.cs)
/// </summary>
public abstract class ApiClientBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected readonly HttpClient Http;

    protected ApiClientBase(HttpClient http)
    {
        Http = http;
    }

    protected Task<ApiResult<T>> GetAsync<T>(string url)
        => SendAsync<T>(() => Http.GetAsync(url));

    protected Task<ApiResult<T>> PostAsync<T>(string url, object body)
        => SendAsync<T>(() => Http.PostAsJsonAsync(url, body, JsonOptions));

    protected Task<ApiResult<T>> PostContentAsync<T>(string url, HttpContent content)
        => SendAsync<T>(() => Http.PostAsync(url, content));

    protected Task<ApiResult<T>> PutAsync<T>(string url, object? body = null)
        => SendAsync<T>(() => Http.PutAsJsonAsync(url, body ?? new { }, JsonOptions));

    protected Task<ApiResult<T>> PatchAsync<T>(string url, object body)
        => SendAsync<T>(() => Http.PatchAsJsonAsync(url, body, JsonOptions));

    protected Task<ApiResult<T>> DeleteAsync<T>(string url)
        => SendAsync<T>(() => Http.DeleteAsync(url));

    /// <summary>Gọi OData và trả về danh sách + tổng số dòng ($count=true).</summary>
    protected async Task<ApiResult<PagedResult<T>>> GetODataAsync<T>(string url)
    {
        try
        {
            var body = await Http.GetFromJsonAsync<ODataResponse<T>>(url, JsonOptions) ?? new ODataResponse<T>();
            return new ApiResult<PagedResult<T>>
            {
                Success = true,
                Data = new PagedResult<T>(body.Value, body.Count ?? body.Value.Count)
            };
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<PagedResult<T>>.Fail(ex.StatusCode.HasValue
                ? $"Truy vấn OData lỗi ({(int)ex.StatusCode})."
                : ConnectionError, ex.StatusCode);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<PagedResult<T>>.Fail(TimeoutError);
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();

            ApiResponse<T>? body = null;
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
                body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);

            if (body != null)
            {
                return new ApiResult<T>
                {
                    Success = response.IsSuccessStatusCode && body.Success,
                    Data = body.Data,
                    Message = body.Message,
                    Errors = body.Errors,
                    StatusCode = response.StatusCode
                };
            }

            return response.IsSuccessStatusCode
                ? new ApiResult<T> { Success = true, StatusCode = response.StatusCode }
                : ApiResult<T>.Fail($"API trả về lỗi {(int)response.StatusCode}.", response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Fail(ConnectionError);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Fail(TimeoutError);
        }
    }

    /// <summary>Ảnh lưu ở API (/uploads/..., /images/...) → URL tuyệt đối để trình duyệt tải được.</summary>
    protected string? ResolveImageUrl(string? url)
        => !string.IsNullOrEmpty(url) && url.StartsWith('/') ? new Uri(Http.BaseAddress!, url).ToString() : url;

    private const string ConnectionError = "Không kết nối được tới máy chủ API. Hãy chắc chắn MotoRent.API đang chạy.";
    private const string TimeoutError = "Máy chủ API phản hồi quá lâu, vui lòng thử lại.";

    private class ODataResponse<T>
    {
        [JsonPropertyName("@odata.count")]
        public int? Count { get; set; }

        [JsonPropertyName("value")]
        public List<T> Value { get; set; } = new();
    }
}
