using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class MaintenanceRecord
{
    public int MaintenanceId { get; set; }

    public int MotorbikeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Description { get; set; } = null!;

    public decimal Cost { get; set; }

    public string Status { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Motorbike Motorbike { get; set; } = null!;
}
