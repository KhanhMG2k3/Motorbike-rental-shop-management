using System.Net.Http.Headers;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Tự động trích xuất JWT Token từ ClaimsIdentity (claim "access_token")
/// và gắn vào Header Authorization: Bearer <token> cho request gọi API.
/// </summary>
public class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var token = user?.FindFirst("access_token")?.Value;

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
