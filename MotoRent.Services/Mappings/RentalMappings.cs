using System.Linq.Expressions;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.Services.Mappings;

public static class RentalMappings
{
    /// <summary>
    /// Expression dùng trong .Select(...) để EF Core dịch thẳng sang SQL (đúng quy ước CLAUDE.md).
    /// </summary>
    public static readonly Expression<Func<Rental, RentalDto>> RentalToDto = r => new RentalDto
    {
        RentalId = r.RentalId,
        RentalCode = r.RentalCode,
        CustomerId = r.CustomerId,
        CustomerName = r.Customer.FullName,
        CustomerEmail = r.Customer.Email,
        CustomerPhone = r.Customer.Phone,
        CustomerIdentityNumber = r.Customer.IdentityNumber,
        CustomerDriverLicense = r.Customer.DriverLicenseNumber,
        StaffId = r.StaffId,
        StaffName = r.Staff != null ? r.Staff.FullName : null,
        BranchId = r.BranchId,
        BranchName = r.Branch.BranchName,
        StartDate = r.StartDate,
        ExpectedReturnDate = r.ExpectedReturnDate,
        ActualReturnDate = r.ActualReturnDate,
        Status = r.Status,
        SubTotal = r.SubTotal,
        DiscountAmount = r.DiscountAmount,
        LateFee = r.LateFee,
        DamageFee = r.DamageFee,
        TotalAmount = r.TotalAmount,
        DepositAmount = r.DepositAmount,
        Note = r.Note,
        CancelReason = r.CancelReason,
        CreatedAt = r.CreatedAt,
        Details = r.RentalDetails.Select(d => new RentalDetailDto
        {
            RentalDetailId = d.RentalDetailId,
            MotorbikeId = d.MotorbikeId,
            LicensePlate = d.Motorbike.LicensePlate,
            ModelName = d.Motorbike.ModelName,
            ImageUrl = d.Motorbike.ImageUrl,
            PricePerDay = d.PricePerDay,
            NumberOfDays = d.NumberOfDays,
            LineTotal = d.LineTotal,
            DepositAmount = d.DepositAmount,
            PickupOdometer = d.PickupOdometer,
            ReturnOdometer = d.ReturnOdometer,
            PickupCondition = d.PickupCondition,
            ReturnCondition = d.ReturnCondition
        }).ToList(),
        Payments = r.Payments.Select(p => new RentalPaymentDto
        {
            PaymentId = p.PaymentId,
            Amount = p.Amount,
            PaymentType = p.PaymentType,
            PaymentMethod = p.PaymentMethod,
            Status = p.Status,
            TransactionRef = p.TransactionRef,
            PaidAt = p.PaidAt,
            Note = p.Note
        }).ToList()
    };

    public static RentalDto ToDto(this Rental r)
    {
        return new RentalDto
        {
            RentalId = r.RentalId,
            RentalCode = r.RentalCode,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.FullName ?? string.Empty,
            CustomerEmail = r.Customer?.Email ?? string.Empty,
            CustomerPhone = r.Customer?.Phone,
            CustomerIdentityNumber = r.Customer?.IdentityNumber,
            CustomerDriverLicense = r.Customer?.DriverLicenseNumber,
            StaffId = r.StaffId,
            StaffName = r.Staff?.FullName,
            BranchId = r.BranchId,
            BranchName = r.Branch?.BranchName ?? string.Empty,
            StartDate = r.StartDate,
            ExpectedReturnDate = r.ExpectedReturnDate,
            ActualReturnDate = r.ActualReturnDate,
            Status = r.Status,
            SubTotal = r.SubTotal,
            DiscountAmount = r.DiscountAmount,
            LateFee = r.LateFee,
            DamageFee = r.DamageFee,
            TotalAmount = r.TotalAmount,
            DepositAmount = r.DepositAmount,
            Note = r.Note,
            CancelReason = r.CancelReason,
            CreatedAt = r.CreatedAt,
            Details = r.RentalDetails.Select(d => new RentalDetailDto
            {
                RentalDetailId = d.RentalDetailId,
                MotorbikeId = d.MotorbikeId,
                LicensePlate = d.Motorbike?.LicensePlate ?? string.Empty,
                ModelName = d.Motorbike?.ModelName ?? string.Empty,
                ImageUrl = d.Motorbike?.ImageUrl,
                PricePerDay = d.PricePerDay,
                NumberOfDays = d.NumberOfDays,
                LineTotal = d.LineTotal,
                DepositAmount = d.DepositAmount,
                PickupOdometer = d.PickupOdometer,
                ReturnOdometer = d.ReturnOdometer,
                PickupCondition = d.PickupCondition,
                ReturnCondition = d.ReturnCondition
            }).ToList(),
            Payments = r.Payments.Select(p => new RentalPaymentDto
            {
                PaymentId = p.PaymentId,
                Amount = p.Amount,
                PaymentType = p.PaymentType,
                PaymentMethod = p.PaymentMethod,
                Status = p.Status,
                TransactionRef = p.TransactionRef,
                PaidAt = p.PaidAt,
                Note = p.Note
            }).ToList()
        };
    }
}