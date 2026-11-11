using System;
using System.Collections.Generic;

namespace MotoRent.BusinessObjects.Entities;

public partial class Brand
{
    public int BrandId { get; set; }

    public string BrandName { get; set; } = null!;

    public string? Country { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Motorbike> Motorbikes { get; set; } = new List<Motorbike>();
}
