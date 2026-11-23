using System;

namespace MotoRent.BusinessObjects.Entities;

public partial class PasswordResetToken
{
    public int ResetTokenId { get; set; }

    public int UserId { get; set; }

    public string CodeHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
