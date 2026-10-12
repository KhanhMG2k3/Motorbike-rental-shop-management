using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.DataAccess;

public partial class MotorbikeRentalContext : DbContext
{
    public MotorbikeRentalContext(DbContextOptions<MotorbikeRentalContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    public virtual DbSet<Motorbike> Motorbikes { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Rental> Rentals { get; set; }

    public virtual DbSet<RentalDetail> RentalDetails { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwMonthlyRevenue> VwMonthlyRevenues { get; set; }

    public virtual DbSet<VwMotorbikeStatistic> VwMotorbikeStatistics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(e => e.BranchName, "UQ_Branches_BranchName").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.CloseTime)
                .HasPrecision(0)
                .HasDefaultValue(new TimeOnly(21, 0, 0));
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OpenTime)
                .HasPrecision(0)
                .HasDefaultValue(new TimeOnly(7, 0, 0));
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasIndex(e => e.BrandName, "UQ_Brands_BrandName").IsUnique();

            entity.Property(e => e.BrandName).HasMaxLength(50);
            entity.Property(e => e.Country).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.CategoryName, "UQ_Categories_CategoryName").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.MaintenanceId);

            entity.HasIndex(e => e.MotorbikeId, "IX_Maintenance_MotorbikeId");

            entity.Property(e => e.Cost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Scheduled");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Maintenance_Users");

            entity.HasOne(d => d.Motorbike).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(d => d.MotorbikeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_Motorbikes");
        });

        modelBuilder.Entity<Motorbike>(entity =>
        {
            entity.HasIndex(e => e.BranchId, "IX_Motorbikes_BranchId");

            entity.HasIndex(e => e.BrandId, "IX_Motorbikes_BrandId");

            entity.HasIndex(e => e.CategoryId, "IX_Motorbikes_CategoryId");

            entity.HasIndex(e => e.Status, "IX_Motorbikes_Status");

            entity.HasIndex(e => e.LicensePlate, "UQ_Motorbikes_LicensePlate").IsUnique();

            entity.Property(e => e.Color).HasMaxLength(30);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.PricePerDay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Available");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Branch).WithMany(p => p.Motorbikes)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Motorbikes_Branches");

            entity.HasOne(d => d.Brand).WithMany(p => p.Motorbikes)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Motorbikes_Brands");

            entity.HasOne(d => d.Category).WithMany(p => p.Motorbikes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Motorbikes_Categories");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.RentalId, "IX_Payments_RentalId");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.PaidAt).HasPrecision(0);
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PaymentType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionRef)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ProcessedBy)
                .HasConstraintName("FK_Payments_Users");

            entity.HasOne(d => d.Rental).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RentalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Rentals");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token, "UQ_RefreshTokens_Token").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.RevokedAt).HasPrecision(0);
            entity.Property(e => e.Token)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasIndex(e => e.CustomerId, "IX_Rentals_CustomerId");

            entity.HasIndex(e => new { e.StartDate, e.ExpectedReturnDate }, "IX_Rentals_StartDate");

            entity.HasIndex(e => e.Status, "IX_Rentals_Status");

            entity.HasIndex(e => e.RentalCode, "UQ_Rentals_RentalCode").IsUnique();

            entity.Property(e => e.ActualReturnDate).HasPrecision(0);
            entity.Property(e => e.CancelReason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DamageFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExpectedReturnDate).HasPrecision(0);
            entity.Property(e => e.LateFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.RentalCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StartDate).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount)
                .HasComputedColumnSql("((([SubTotal]-[DiscountAmount])+[LateFee])+[DamageFee])", true)
                .HasColumnType("decimal(21, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Branch).WithMany(p => p.Rentals)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rentals_Branches");

            entity.HasOne(d => d.Customer).WithMany(p => p.RentalCustomers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rentals_Customer");

            entity.HasOne(d => d.Staff).WithMany(p => p.RentalStaffs)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_Rentals_Staff");
        });

        modelBuilder.Entity<RentalDetail>(entity =>
        {
            entity.HasIndex(e => e.MotorbikeId, "IX_RentalDetails_MotorbikeId");

            entity.HasIndex(e => new { e.RentalId, e.MotorbikeId }, "UQ_RentalDetails_Rental_Motorbike").IsUnique();

            entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("([PricePerDay]*[NumberOfDays])", true)
                .HasColumnType("decimal(29, 2)");
            entity.Property(e => e.PickupCondition).HasMaxLength(500);
            entity.Property(e => e.PricePerDay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReturnCondition).HasMaxLength(500);

            entity.HasOne(d => d.Motorbike).WithMany(p => p.RentalDetails)
                .HasForeignKey(d => d.MotorbikeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RentalDetails_Motorbikes");

            entity.HasOne(d => d.Rental).WithMany(p => p.RentalDetails)
                .HasForeignKey(d => d.RentalId)
                .HasConstraintName("FK_RentalDetails_Rentals");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => e.MotorbikeId, "IX_Reviews_MotorbikeId");

            entity.HasIndex(e => new { e.RentalId, e.MotorbikeId }, "UQ_Reviews_Rental_Motorbike").IsUnique();

            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Users");

            entity.HasOne(d => d.Motorbike).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.MotorbikeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Motorbikes");

            entity.HasOne(d => d.Rental).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.RentalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Rentals");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.IdentityNumber, "UX_Users_IdentityNumber")
                .IsUnique()
                .HasFilter("([IdentityNumber] IS NOT NULL)");

            entity.HasIndex(e => e.Phone, "UX_Users_Phone")
                .IsUnique()
                .HasFilter("([Phone] IS NOT NULL)");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DriverLicenseNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdentityNumber)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Branch).WithMany(p => p.Users)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_Users_Branches");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<VwMonthlyRevenue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MonthlyRevenue");

            entity.Property(e => e.PenaltyRevenue).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.RentalRevenue).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwMotorbikeStatistic>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MotorbikeStatistics");

            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.BrandName).HasMaxLength(50);
            entity.Property(e => e.CategoryName).HasMaxLength(50);
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.PricePerDay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TotalEarned).HasColumnType("decimal(38, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
