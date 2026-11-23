using System.ComponentModel.DataAnnotations;

namespace MotoRent.BusinessObjects.DTOs.Maintenances;

public class MaintenanceDto
{
    public int MaintenanceId { get; set; }
    public int MotorbikeId { get; set; }
    public string LicensePlate { get; set; } = null!;
    public string ModelName { get; set; } = null!;
    public string MotorbikeStatus { get; set; } = null!;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Description { get; set; } = null!;
    public decimal Cost { get; set; }
    public string Status { get; set; } = null!;
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Sửa phiếu bảo dưỡng (khi còn Scheduled / InProgress).</summary>
public class UpdateMaintenanceDto : IValidatableObject
{
    public const int MaxDaysInPast = 30;

    [Display(Name = "Ngày bắt đầu")]
    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateOnly? StartDate { get; set; }

    /// <summary>Ngày dự kiến xong (có thể bỏ trống).</summary>
    [Display(Name = "Ngày dự kiến xong")]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "Nội dung bảo dưỡng")]
    [Required(ErrorMessage = "Vui lòng nhập nội dung bảo dưỡng.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Nội dung từ 5 đến 500 ký tự.")]
    public string Description { get; set; } = null!;

    [Display(Name = "Chi phí (dự kiến)")]
    [Range(0, 100000000, ErrorMessage = "Chi phí từ 0 đến 100.000.000 đ.")]
    public decimal Cost { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            yield return new ValidationResult("Ngày dự kiến xong phải từ ngày bắt đầu trở đi.", new[] { nameof(EndDate) });

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (StartDate.HasValue && StartDate < today.AddDays(-MaxDaysInPast))
            yield return new ValidationResult($"Ngày bắt đầu không được trước hôm nay quá {MaxDaysInPast} ngày.", new[] { nameof(StartDate) });
    }
}

public class CreateMaintenanceDto : UpdateMaintenanceDto
{
    [Display(Name = "Xe")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn xe.")]
    public int MotorbikeId { get; set; }
}

/// <summary>Hoàn thành phiếu bảo dưỡng: ghi chi phí thực tế và ngày xong.</summary>
public class CompleteMaintenanceDto
{
    /// <summary>Bỏ trống = hôm nay.</summary>
    [Display(Name = "Ngày hoàn thành")]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "Chi phí thực tế")]
    [Range(0, 100000000, ErrorMessage = "Chi phí từ 0 đến 100.000.000 đ.")]
    public decimal Cost { get; set; }
}

/// <summary>Bộ lọc danh sách phiếu bảo dưỡng: GET /api/maintenances?motorbikeId=&amp;status=&amp;branchId=</summary>
public class MaintenanceQuery
{
    public int? MotorbikeId { get; set; }
    public int? BranchId { get; set; }

    [RegularExpression("^(Scheduled|InProgress|Completed|Cancelled)$",
        ErrorMessage = "Trạng thái chỉ được là Scheduled, InProgress, Completed hoặc Cancelled.")]
    public string? Status { get; set; }
}
