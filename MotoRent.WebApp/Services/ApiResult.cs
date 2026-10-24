using System.Net;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MotoRent.WebApp.Services;

/// <summary>Kết quả gọi API đã "mở gói" ApiResponse, kèm HTTP status.</summary>
public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
    public HttpStatusCode? StatusCode { get; set; }

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;

    public static ApiResult<T> Fail(string message, HttpStatusCode? statusCode = null)
        => new() { Success = false, Message = message, StatusCode = statusCode };

    /// <summary>
    /// Đưa lỗi từ API vào ModelState để hiện ngay dưới ô nhập tương ứng.
    /// prefix: tên thuộc tính chứa DTO trong ViewModel (VD: "Form").
    /// </summary>
    public void AddErrorsTo(ModelStateDictionary modelState, string prefix = "")
    {
        if (Errors is { Count: > 0 })
        {
            foreach (var (key, messages) in Errors)
            {
                var field = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
                foreach (var message in messages)
                    modelState.AddModelError(field, message);
            }
        }
        else
        {
            modelState.AddModelError(string.Empty, Message ?? "Đã xảy ra lỗi.");
        }
    }
}

public record PagedResult<T>(List<T> Items, int TotalCount);
