using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class Rental
{
    public int RentalId { get; set; }

    public string RentalCode { get; set; } = null!;

    public int CustomerId { get; set; }

    public int? StaffId { get; set; }

    public int BranchId { get; set; }

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

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User Customer { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RentalDetail> RentalDetails { get; set; } = new List<RentalDetail>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User? Staff { get; set; }
}
