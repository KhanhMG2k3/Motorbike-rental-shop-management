using System.ComponentModel.DataAnnotations;

namespace MotoRent.BusinessObjects.DTOs.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; set; } = null!;
}

public class RegisterDto : IValidatableObject
{
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên từ 2 đến 100 ký tự.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = null!;

    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải từ 10–11 chữ số và bắt đầu bằng số 0.")]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string? Address { get; set; }

    [RegularExpression(@"^\d{12}$", ErrorMessage = "Số CCCD phải gồm đúng 12 chữ số.")]
    public string? IdentityNumber { get; set; }

    [StringLength(20, ErrorMessage = "Số GPLX tối đa 20 ký tự.")]
    public string? DriverLicenseNumber { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value > today.AddYears(-age)) age--;

            if (age < 18)
            {
                yield return new ValidationResult(
                    "Khách hàng phải từ đủ 18 tuổi trở lên.",
                    new[] { nameof(DateOfBirth) });
            }
        }
    }
}

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int? BranchId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? IdentityNumber { get; set; }
    public string? DriverLicenseNumber { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto : IValidatableObject
{
    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên từ 2 đến 100 ký tự.")]
    public string FullName { get; set; } = null!;

    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải từ 10–11 chữ số và bắt đầu bằng số 0.")]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string? Address { get; set; }

    [RegularExpression(@"^\d{12}$", ErrorMessage = "Số CCCD phải gồm đúng 12 chữ số.")]
    public string? IdentityNumber { get; set; }

    [StringLength(20, ErrorMessage = "Số GPLX tối đa 20 ký tự.")]
    public string? DriverLicenseNumber { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value > today.AddYears(-age)) age--;

            if (age < 18)
            {
                yield return new ValidationResult(
                    "Khách hàng phải từ đủ 18 tuổi trở lên.",
                    new[] { nameof(DateOfBirth) });
            }
        }
    }
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmNewPassword { get; set; } = null!;
}
