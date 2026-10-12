using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class RentalDetail
{
    public int RentalDetailId { get; set; }

    public int RentalId { get; set; }

    public int MotorbikeId { get; set; }

    public decimal PricePerDay { get; set; }

    public int NumberOfDays { get; set; }

    public decimal? LineTotal { get; set; }

    public decimal DepositAmount { get; set; }

    public int? PickupOdometer { get; set; }

    public int? ReturnOdometer { get; set; }

    public string? PickupCondition { get; set; }

    public string? ReturnCondition { get; set; }

    public virtual Motorbike Motorbike { get; set; } = null!;

    public virtual Rental Rental { get; set; } = null!;
}
