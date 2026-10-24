using System.ComponentModel.DataAnnotations;

namespace MotoRent.BusinessObjects.DTOs.Branches;

public class BranchDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MotorbikeCount { get; set; }
    public int StaffCount { get; set; }
}

public class CreateBranchDto : IValidatableObject
{
    [Required(ErrorMessage = "Tên chi nhánh không được để trống.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên chi nhánh từ 2 đến 100 ký tự.")]
    public string BranchName { get; set; } = null!;

    [Required(ErrorMessage = "Địa chỉ không được để trống.")]
    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải từ 10–11 chữ số và bắt đầu bằng số 0.")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Giờ mở cửa không được để trống.")]
    public TimeOnly OpenTime { get; set; } = new(7, 0);

    [Required(ErrorMessage = "Giờ đóng cửa không được để trống.")]
    public TimeOnly CloseTime { get; set; } = new(21, 0);

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CloseTime <= OpenTime)
        {
            yield return new ValidationResult(
                "Giờ đóng cửa phải sau giờ mở cửa.",
                new[] { nameof(CloseTime) });
        }
    }
}

public class UpdateBranchDto : CreateBranchDto
{
    public bool IsActive { get; set; } = true;
}
