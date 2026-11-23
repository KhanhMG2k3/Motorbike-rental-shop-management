using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class Review
{
    public int ReviewId { get; set; }

    public int RentalId { get; set; }

    public int MotorbikeId { get; set; }

    public int CustomerId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual Motorbike Motorbike { get; set; } = null!;

    public virtual Rental Rental { get; set; } = null!;
}
