using MotoRent.BusinessObjects.Entities;

namespace MotoRent.DataAccess.Repositories;

public interface IMotorbikeRepository : IGenericRepository<Motorbike>
{
    /// <summary>
    /// Xe có thể cho thuê trong khoảng [start, end): trạng thái Available/Rented
    /// và không trùng lịch với đơn Pending/Confirmed/Renting nào.
    /// </summary>
    IQueryable<Motorbike> QueryAvailable(DateTime start, DateTime end);

    /// <summary>
    /// Kiểm tra 1 xe có trống trong khoảng thời gian không.
    /// Dev 3 dùng khi tạo / sửa đơn thuê (truyền excludeRentalId khi sửa đơn).
    /// </summary>
    Task<bool> IsAvailableAsync(int motorbikeId, DateTime start, DateTime end, int? excludeRentalId = null);

    Task<bool> LicensePlateExistsAsync(string licensePlate, int? excludeMotorbikeId = null);

    /// <summary>Xe đang nằm trong đơn Pending/Confirmed/Renting.</summary>
    Task<bool> HasActiveRentalAsync(int motorbikeId);
}
