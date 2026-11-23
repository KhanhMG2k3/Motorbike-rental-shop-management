using System.ComponentModel.DataAnnotations;
using MotoRent.BusinessObjects.Validation;

namespace MotoRent.BusinessObjects.DTOs.Motorbikes;

/// <summary>
/// DTO trả về cho client, đã "làm phẳng" tên hãng / loại / chi nhánh.
/// Đồng thời là kiểu dữ liệu của OData endpoint /odata/Motorbikes.
/// </summary>
public class MotorbikeDto
{
    public int MotorbikeId { get; set; }
    public string LicensePlate { get; set; } = null!;
    public string ModelName { get; set; } = null!;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = null!;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public int ManufactureYear { get; set; }
    public int? EngineCapacity { get; set; }
    public string Color { get; set; } = null!;
    public decimal PricePerDay { get; set; }
    public decimal DepositAmount { get; set; }
    public int Odometer { get; set; }
    public string Status { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class CreateMotorbikeDto
{
    // Hợp lệ: 59A1-123.45, 29B-123.45, 51F-12345
    [Required(ErrorMessage = "Biển số không được để trống.")]
    [RegularExpression(@"^[0-9]{2}[A-Za-z][A-Za-z0-9]?-([0-9]{3}\.[0-9]{2}|[0-9]{4,5})$",
        ErrorMessage = "Biển số không đúng định dạng (VD: 59A1-123.45).")]
    public string LicensePlate { get; set; } = null!;

    [Required(ErrorMessage = "Tên mẫu xe không được để trống.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên mẫu xe từ 2 đến 100 ký tự.")]
    public string ModelName { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hãng xe.")]
    public int BrandId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại xe.")]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chi nhánh.")]
    public int BranchId { get; set; }

    [ManufactureYear]
    public int ManufactureYear { get; set; }

    /// <summary>Dung tích xi-lanh (cc). Để trống với xe điện.</summary>
    [Range(50, 1500, ErrorMessage = "Dung tích xi-lanh từ 50 đến 1500 cc.")]
    public int? EngineCapacity { get; set; }

    [Required(ErrorMessage = "Màu xe không được để trống.")]
    [StringLength(30, ErrorMessage = "Màu xe tối đa 30 ký tự.")]
    public string Color { get; set; } = null!;

    [Range(10000, 10000000, ErrorMessage = "Giá thuê/ngày từ 10.000 đến 10.000.000 đ.")]
    public decimal PricePerDay { get; set; }

    [Range(0, 100000000, ErrorMessage = "Tiền cọc từ 0 đến 100.000.000 đ.")]
    public decimal DepositAmount { get; set; }

    [Range(0, 1000000, ErrorMessage = "Số km không hợp lệ.")]
    public int Odometer { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
    public string? ImageUrl { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }
}

public class UpdateMotorbikeDto : CreateMotorbikeDto
{
}

public class UpdateMotorbikeStatusDto
{
    // "Rented" chỉ do luồng thuê xe đặt, không cho đổi tay
    [Required(ErrorMessage = "Trạng thái không được để trống.")]
    [RegularExpression("^(Available|Maintenance|Inactive)$",
        ErrorMessage = "Trạng thái chỉ được là Available, Maintenance hoặc Inactive.")]
    public string Status { get; set; } = null!;
}

/// <summary>
/// Tham số tìm xe trống: GET /api/motorbikes/available?startDate=...&amp;endDate=...
/// </summary>
public class AvailableMotorbikeQuery : IValidatableObject
{
    public const int MaxRentalDays = 90;

    [Required(ErrorMessage = "Vui lòng chọn ngày nhận xe.")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày trả xe.")]
    public DateTime? EndDate { get; set; }

    public int? BranchId { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate is null || EndDate is null) yield break;

        if (EndDate <= StartDate)
            yield return new ValidationResult("Ngày trả phải sau ngày nhận.", new[] { nameof(EndDate) });

        if ((EndDate.Value - StartDate.Value).TotalDays > MaxRentalDays)
            yield return new ValidationResult($"Thời gian thuê tối đa {MaxRentalDays} ngày.", new[] { nameof(EndDate) });

        if (StartDate.Value < DateTime.Now.AddHours(-1))
            yield return new ValidationResult("Ngày nhận xe không được ở quá khứ.", new[] { nameof(StartDate) });
    }
}
