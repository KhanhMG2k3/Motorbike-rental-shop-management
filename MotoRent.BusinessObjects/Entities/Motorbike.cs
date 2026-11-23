using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class Motorbike
{
    public int MotorbikeId { get; set; }

    public string LicensePlate { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public int BrandId { get; set; }

    public int CategoryId { get; set; }

    public int BranchId { get; set; }

    public int ManufactureYear { get; set; }

    public int? EngineCapacity { get; set; }

    public string Color { get; set; } = null!;

    public decimal PricePerDay { get; set; }

    public decimal DepositAmount { get; set; }

    public int Odometer { get; set; }

    public string Status { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Brand Brand { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    public virtual ICollection<RentalDetail> RentalDetails { get; set; } = new List<RentalDetail>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
