using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int RentalId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentType { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? TransactionRef { get; set; }

    public DateTime? PaidAt { get; set; }

    public int? ProcessedBy { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual Rental Rental { get; set; } = null!;
}
