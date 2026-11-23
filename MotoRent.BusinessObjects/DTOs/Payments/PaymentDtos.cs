using System.ComponentModel.DataAnnotations;

namespace MotoRent.BusinessObjects.DTOs.Payments;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int RentalId { get; set; }
    public string RentalCode { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? TransactionRef { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? ProcessedBy { get; set; }
    public string? ProcessedByName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Danh sách thanh toán có phân trang + tổng hợp tiền (chỉ tính các khoản đã Paid).</summary>
public class PaymentListDto
{
    public List<PaymentDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    /// <summary>Tiền đã thu: Deposit + RentalFee + Penalty.</summary>
    public decimal TotalCollected { get; set; }

    /// <summary>Tiền đã hoàn trả khách (Refund).</summary>
    public decimal TotalRefunded { get; set; }

    /// <summary>Doanh thu: RentalFee + Penalty (tiền cọc chỉ là tiền giữ hộ).</summary>
    public decimal Revenue { get; set; }
}

/// <summary>Bộ lọc: GET /api/payments?rentalId=&amp;type=&amp;method=&amp;status=&amp;from=&amp;to=&amp;page=&amp;pageSize=</summary>
public class PaymentQuery : IValidatableObject
{
    public const int MaxPageSize = 50;

    public int? RentalId { get; set; }

    [RegularExpression("^(Deposit|RentalFee|Penalty|Refund)$", ErrorMessage = "Loại thanh toán chỉ được là Deposit, RentalFee, Penalty hoặc Refund.")]
    public string? Type { get; set; }

    [RegularExpression("^(Cash|BankTransfer|Momo|VNPay)$", ErrorMessage = "Phương thức chỉ được là Cash, BankTransfer, Momo hoặc VNPay.")]
    public string? Method { get; set; }

    [RegularExpression("^(Pending|Paid|Failed|Cancelled)$", ErrorMessage = "Trạng thái chỉ được là Pending, Paid, Failed hoặc Cancelled.")]
    public string? Status { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Trang phải từ 1 trở lên.")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Số dòng mỗi trang từ 1 đến 50.")]
    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && To.Value.Date < From.Value.Date)
            yield return new ValidationResult("Ngày kết thúc phải từ ngày bắt đầu trở đi.", new[] { nameof(To) });
    }
}

public class ConfirmPaymentDto
{
    [StringLength(100, ErrorMessage = "Mã giao dịch tối đa 100 ký tự.")]
    public string? TransactionRef { get; set; }

    [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }
}

public class CancelPaymentDto
{
    [Required(ErrorMessage = "Vui lòng nhập lý do huỷ khoản thanh toán.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Lý do từ 3 đến 255 ký tự.")]
    public string Reason { get; set; } = null!;
}
