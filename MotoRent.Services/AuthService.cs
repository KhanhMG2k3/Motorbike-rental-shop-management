using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Auth;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

public class AuthService : IAuthService
{
    private readonly IGenericRepository<User> _userRepo;
    private readonly IConfiguration _config;

    public AuthService(IGenericRepository<User> userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _userRepo.Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new BusinessException("Email hoặc mật khẩu không chính xác.");

        if (!user.IsActive)
            throw new BusinessException("Tài khoản đã bị khoá. Vui lòng liên hệ quản trị viên.");

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var exists = await _userRepo.Query().AnyAsync(u => u.Email == email);
        if (exists)
            throw new ConflictException("Email này đã được đăng ký.");

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var phone = dto.Phone.Trim();
            if (await _userRepo.Query().AnyAsync(u => u.Phone == phone))
                throw new ConflictException("Số điện thoại này đã được sử dụng.");
        }

        if (!string.IsNullOrWhiteSpace(dto.IdentityNumber))
        {
            var idNumber = dto.IdentityNumber.Trim();
            if (await _userRepo.Query().AnyAsync(u => u.IdentityNumber == idNumber))
                throw new ConflictException("Số CCCD này đã được sử dụng.");
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = dto.Phone?.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Address = dto.Address?.Trim(),
            IdentityNumber = dto.IdentityNumber?.Trim(),
            DriverLicenseNumber = dto.DriverLicenseNumber?.Trim(),
            RoleId = 3, // Customer
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        // Load Role để sinh token
        user.Role = new Role { RoleId = 3, RoleName = AppRoles.Customer };
        return GenerateAuthResponse(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        return await _userRepo.Query()
            .Where(u => u.UserId == userId)
            .Select(UserMappings.UserToProfileDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var phone = dto.Phone.Trim();
            if (await _userRepo.Query().AnyAsync(u => u.Phone == phone && u.UserId != userId))
                throw new ConflictException("Số điện thoại này đã được tài khoản khác sử dụng.");
        }

        if (!string.IsNullOrWhiteSpace(dto.IdentityNumber))
        {
            var idNumber = dto.IdentityNumber.Trim();
            if (await _userRepo.Query().AnyAsync(u => u.IdentityNumber == idNumber && u.UserId != userId))
                throw new ConflictException("Số CCCD này đã được tài khoản khác sử dụng.");
        }

        dto.ApplyTo(user);
        await _userRepo.SaveChangesAsync();

        return await GetProfileAsync(userId);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new BusinessException("Mật khẩu hiện tại không chính xác.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.Now;
        await _userRepo.SaveChangesAsync();
    }

    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Key trong appsettings.json");
        var issuer = _config["Jwt:Issuer"] ?? "MotoRent.API";
        var audience = _config["Jwt:Audience"] ?? "MotoRent.Client";
        var expireMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var exp) ? exp : 1440;

        var tokenHandler = new JwtSecurityTokenHandler();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var expiresAt = DateTime.UtcNow.AddMinutes(expireMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role?.RoleName ?? AppRoles.Customer)
        };

        if (user.BranchId.HasValue)
        {
            claims.Add(new Claim("branch_id", user.BranchId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AuthResponseDto
        {
            Token = tokenHandler.WriteToken(token),
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role?.RoleName ?? AppRoles.Customer,
            BranchId = user.BranchId,
            ExpiresAt = expiresAt
        };
    }
}
