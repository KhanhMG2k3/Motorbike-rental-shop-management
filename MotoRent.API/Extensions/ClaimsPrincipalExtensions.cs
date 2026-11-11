using System.Security.Claims;

namespace MotoRent.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// UserId của người đang đăng nhập (claim NameIdentifier trong JWT).
    /// Trả về null khi chưa đăng nhập — chạy được cả trước khi Dev 1 cấu hình JWT.
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
