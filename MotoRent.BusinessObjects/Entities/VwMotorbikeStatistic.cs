using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class VwMotorbikeStatistic
{
    public int MotorbikeId { get; set; }

    public string LicensePlate { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public string BrandName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string BranchName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal PricePerDay { get; set; }

    public int? CompletedRentals { get; set; }

    public decimal? TotalEarned { get; set; }

    public decimal? AverageRating { get; set; }
}
