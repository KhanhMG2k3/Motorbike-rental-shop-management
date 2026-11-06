using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MotoRent.BusinessObjects.DTOs.Rentals
{
    public class RentalDto
    {
        public int RentalId { get; set; }
        public string RentalCode { get; set; } = null!;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string? CustomerPhone { get; set; }
        public string? CustomerIdentityNumber { get; set; }
        public string? CustomerDriverLicense { get; set; }
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LateFee { get; set; }
        public decimal DamageFee { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public string? Note { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RentalDetailDto> Details { get; set; } = new();
        public List<RentalPaymentDto> Payments { get; set; } = new();
    }

    public class RentalDetailDto
    {
        public int RentalDetailId { get; set; }
        public int MotorbikeId { get; set; }
        public string LicensePlate { get; set; } = null!;
        public string ModelName { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public decimal PricePerDay { get; set; }
        public int NumberOfDays { get; set; }
        public decimal? LineTotal { get; set; }
        public decimal DepositAmount { get; set; }
        public int? PickupOdometer { get; set; }
        public int? ReturnOdometer { get; set; }
        public string? PickupCondition { get; set; }
        public string? ReturnCondition { get; set; }
    }
    public class RentalPaymentDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? TransactionRef { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Note { get; set; }
    }
    public class CreateRentalDto : IValidatableObject
    {
        public const int MaxRentalDays = 90;

        /// <summary>
        /// Chỉ dùng khi Staff đặt hộ khách. Customer tự đặt thì để null (hoặc bỏ qua).
        /// </summary>
        public int? CustomerId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chi nhánh nhận xe.")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận xe.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả xe.")]
        public DateTime ExpectedReturnDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 xe để thuê.")]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 xe để thuê.")]
        public List<int> MotorbikeIds { get; set; } = new();

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExpectedReturnDate <= StartDate)
            {
                yield return new ValidationResult("Thời điểm hẹn trả phải sau thời điểm nhận xe.", new[] { nameof(ExpectedReturnDate) });
            }
            if ((ExpectedReturnDate - StartDate).TotalDays > MaxRentalDays)
            {
                yield return new ValidationResult($"Thời gian thuê xe tối đa không quá {MaxRentalDays} ngày.", new[] { nameof(ExpectedReturnDate) });
            }
            if (StartDate < DateTime.Now.AddHours(-1))
            {
                yield return new ValidationResult("Thời điểm nhận xe không được nằm trong quá khứ.", new[] { nameof(StartDate) });
            }
            if (MotorbikeIds.Distinct().Count() != MotorbikeIds.Count)
            {
                yield return new ValidationResult("Danh sách xe được chọn không được chứa các xe trùng nhau.", new[] { nameof(MotorbikeIds) });
            }
        }
    }

    public class ConfirmRentalDto
    {
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán tiền cọc.")]
        [RegularExpression("^(Cash|BankTransfer|Momo|VNPay)$", ErrorMessage = "Phương thức thanh toán phải là: Cash, BankTransfer, Momo hoặc VNPay.")]
        [DefaultValue("Cash")]
        public string PaymentMethod { get; set; } = "Cash";

        public string? TransactionRef { get; set; }
        public string? Note { get; set; }
    }

    public class PickupRentalDto
    {
        [Required(ErrorMessage = "Vui lòng nhập thông tin biên bản bàn giao xe.")]
        [MinLength(1, ErrorMessage = "Vui lòng nhập thông tin bàn giao cho ít nhất 1 xe.")]
        public List<PickupItemDto> Items { get; set; } = new();
    }

    public class PickupItemDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "MotorbikeId không hợp lệ.")]
        public int MotorbikeId { get; set; }

        [Range(0, 1000000, ErrorMessage = "Số km lúc giao không hợp lệ.")]
        public int PickupOdometer { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả tình trạng xe tối đa 500 ký tự.")]
        public string? PickupCondition { get; set; }
    }

    public class ReturnRentalDto
    {
        public DateTime? ActualReturnDate { get; set; }

        [Range(0, 100000000, ErrorMessage = "Phí hư hỏng không hợp lệ.")]
        public decimal DamageFee { get; set; } = 0;

        [Range(0, 100000000, ErrorMessage = "Số tiền giảm giá không hợp lệ.")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán / hoàn cọc.")]
        [RegularExpression("^(Cash|BankTransfer|Momo|VNPay)$", ErrorMessage = "Phương thức thanh toán phải là: Cash, BankTransfer, Momo hoặc VNPay.")]
        [DefaultValue("Cash")]
        public string PaymentMethod { get; set; } = "Cash";

        [Required(ErrorMessage = "Vui lòng cung cấp thông tin hoàn trả của từng xe.")]
        [MinLength(1, ErrorMessage = "Vui lòng nhập thông tin trả xe cho ít nhất 1 xe.")]
        public List<ReturnItemDto> Items { get; set; } = new();
    }

    public class ReturnItemDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "MotorbikeId không hợp lệ.")]
        public int MotorbikeId { get; set; }

        [Range(0, 1000000, ErrorMessage = "Số km lúc trả không hợp lệ.")]
        public int ReturnOdometer { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả tình trạng xe lúc trả tối đa 500 ký tự.")]
        public string? ReturnCondition { get; set; }

        /// <summary>Nếu đánh dấu true, xe sẽ chuyển sang trạng thái Maintenance để kiểm tra sửa chữa.</summary>
        public bool IsDamaged { get; set; } = false;
    }

    public class CancelRentalDto
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do huỷ đơn thuê.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Lý do huỷ đơn từ 3 đến 500 ký tự.")]
        public string CancelReason { get; set; } = null!;

        /// <summary>Phương thức hoàn cọc nếu đơn đã cọc (mặc định Cash).</summary>
        [Required(ErrorMessage = "Vui lòng chọn phương thức hoàn cọc.")]
        [RegularExpression("^(Cash|BankTransfer|Momo|VNPay)$", ErrorMessage = "Phương thức hoàn cọc phải là: Cash, BankTransfer, Momo hoặc VNPay.")]
        [DefaultValue("Cash")]
        public string RefundMethod { get; set; } = "Cash";
    }
}
