using MotoRent.BusinessObjects.DTOs.Auth;

namespace MotoRent.WebApp.Services;

/// <summary>
/// Gọi API xác thực tài khoản và quản lý hồ sơ cá nhân.
/// </summary>
public class AuthApiClient : ApiClientBase
{
    public AuthApiClient(HttpClient http) : base(http) { }

    public Task<ApiResult<AuthResponseDto>> LoginAsync(LoginDto dto)
        => PostAsync<AuthResponseDto>("api/auth/login", dto);

    public Task<ApiResult<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        => PostAsync<AuthResponseDto>("api/auth/register", dto);

    public Task<ApiResult<UserProfileDto>> GetProfileAsync()
        => GetAsync<UserProfileDto>("api/auth/me");

    public Task<ApiResult<UserProfileDto>> UpdateProfileAsync(UpdateProfileDto dto)
        => PutAsync<UserProfileDto>("api/auth/me", dto);

    public Task<ApiResult<object>> ChangePasswordAsync(ChangePasswordDto dto)
        => PutAsync<object>("api/auth/change-password", dto);
}
