using System.ComponentModel.DataAnnotations;

namespace MotoRent.BusinessObjects.DTOs.Users;

public class UserDto
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

public class CreateStaffDto : IValidatableObject
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

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải từ 10–11 chữ số và bắt đầu bằng số 0.")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Nhân viên phải thuộc một chi nhánh.")]
    public int BranchId { get; set; }

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
                    "Nhân viên phải từ đủ 18 tuổi trở lên.",
                    new[] { nameof(DateOfBirth) });
            }
        }
    }
}

public class UserFilter
{
    public int? RoleId { get; set; }
    public int? BranchId { get; set; }
    public string? Keyword { get; set; }
}
