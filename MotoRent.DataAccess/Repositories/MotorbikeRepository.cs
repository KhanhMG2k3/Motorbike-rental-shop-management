using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.Entities;

namespace MotoRent.DataAccess.Repositories;

public class MotorbikeRepository : GenericRepository<Motorbike>, IMotorbikeRepository
{
    public MotorbikeRepository(MotorbikeRentalContext context) : base(context) { }

    public IQueryable<Motorbike> QueryAvailable(DateTime start, DateTime end)
    {
        // Xe đang Rented vẫn có thể đặt cho khoảng thời gian SAU khi trả,
        // nên chỉ loại Maintenance / Inactive, còn lại dựa vào kiểm tra trùng lịch.
        return Query().Where(m =>
            (m.Status == MotorbikeStatus.Available || m.Status == MotorbikeStatus.Rented) &&
            !m.RentalDetails.Any(d =>
                RentalStatus.Active.Contains(d.Rental.Status) &&
                d.Rental.StartDate < end &&
                start < d.Rental.ExpectedReturnDate));
    }

    public Task<bool> IsAvailableAsync(int motorbikeId, DateTime start, DateTime end, int? excludeRentalId = null)
    {
        return Query().AnyAsync(m =>
            m.MotorbikeId == motorbikeId &&
            (m.Status == MotorbikeStatus.Available || m.Status == MotorbikeStatus.Rented) &&
            !m.RentalDetails.Any(d =>
                d.RentalId != excludeRentalId &&
                RentalStatus.Active.Contains(d.Rental.Status) &&
                d.Rental.StartDate < end &&
                start < d.Rental.ExpectedReturnDate));
    }

    public Task<bool> LicensePlateExistsAsync(string licensePlate, int? excludeMotorbikeId = null)
    {
        return Query().AnyAsync(m =>
            m.LicensePlate == licensePlate &&
            m.MotorbikeId != excludeMotorbikeId);
    }

    public Task<bool> HasActiveRentalAsync(int motorbikeId)
    {
        return _context.RentalDetails.AnyAsync(d =>
            d.MotorbikeId == motorbikeId &&
            RentalStatus.Active.Contains(d.Rental.Status));
    }
}
