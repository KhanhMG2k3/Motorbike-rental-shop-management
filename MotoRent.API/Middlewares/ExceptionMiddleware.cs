using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Common;
using MotoRent.BusinessObjects.Exceptions;

namespace MotoRent.API.Middlewares;

/// <summary>
/// Bắt mọi exception và trả về ApiResponse với HTTP status phù hợp.
/// </summary>
public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // Giữ nguyên tiếng Việt thay vì ô...
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, message) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                BusinessException => (StatusCodes.Status400BadRequest, ex.Message),
                ConflictException => (StatusCodes.Status409Conflict, ex.Message),
                DbUpdateException => (StatusCodes.Status409Conflict, "Dữ liệu vi phạm ràng buộc trong cơ sở dữ liệu."),
                _ => (StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi hệ thống.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception");
            else
                _logger.LogWarning("{Status}: {Message}", status, ex.Message);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<object>.Fail(message), JsonOptions));
        }
    }
}
