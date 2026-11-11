using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class VwMonthlyRevenue
{
    public int? Year { get; set; }

    public int? Month { get; set; }

    public decimal? RentalRevenue { get; set; }

    public decimal? PenaltyRevenue { get; set; }

    public decimal? TotalRevenue { get; set; }

    public int? RentalCount { get; set; }
}
