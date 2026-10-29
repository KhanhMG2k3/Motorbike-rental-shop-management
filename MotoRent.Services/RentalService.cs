using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;
using MotoRent.Services.Mappings;

namespace MotoRent.Services;

public class RentalService : IRentalService
{
    private const int CustomerRoleId = 3;

    private readonly MotorbikeRentalContext _context;
    private readonly IMotorbikeRepository _motorbikeRepo;
    private readonly IGenericRepository<Rental> _rentalRepo;
    private readonly IGenericRepository<User> _userRepo;
    private readonly IGenericRepository<Branch> _branchRepo;

    public RentalService(
        MotorbikeRentalContext context,
        IMotorbikeRepository motorbikeRepo,
        IGenericRepository<Rental> rentalRepo,
        IGenericRepository<User> userRepo,
        IGenericRepository<Branch> branchRepo)
    {
        _context = context;
        _motorbikeRepo = motorbikeRepo;
        _rentalRepo = rentalRepo;
        _userRepo = userRepo;
        _branchRepo = branchRepo;
    }

    public async Task<RentalDto> GetByIdAsync(int id, int? currentUserId = null, string? currentRole = null)
    {
        var rental = await GetDtoByIdAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy đơn thuê có Id = {id}.");

        // Customer chỉ được xem đơn của chính mình
        if (currentRole == AppRoles.Customer && currentUserId.HasValue && rental.CustomerId != currentUserId.Value)
        {
            throw new BusinessException("Bạn không có quyền truy cập vào thông tin đơn thuê này.");
        }

        return rental;
    }

    public Task<List<RentalDto>> GetMyRentalsAsync(int customerId)
    {
        return _rentalRepo.Query()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(RentalMappings.RentalToDto)
            .ToListAsync();
    }

    public async Task<RentalDto> CreateAsync(CreateRentalDto dto, int requestUserId, string userRole)
    {
        // 1. Xác định CustomerId và StaffId theo vai trò người gọi
        int customerId;
        int? staffId;

        if (userRole == AppRoles.Customer)
        {
            // Nếu Customer cố tình truyền CustomerId khác ID của chính mình -> chặn ngay 400
            if (dto.CustomerId.HasValue && dto.CustomerId.Value > 0 && dto.CustomerId.Value != requestUserId)
            {
                throw new BusinessException("Khách hàng chỉ được phép đặt đơn thuê cho chính mình (vui lòng để customerId là null).");
            }

            customerId = requestUserId;
            staffId = null;
        }
        else if (userRole == AppRoles.Staff)
        {
            // Staff tạo đơn hộ khách: bắt buộc truyền CustomerId trong body
            if (!dto.CustomerId.HasValue || dto.CustomerId.Value <= 0)
            {
                throw new BusinessException("Vui lòng chọn khách hàng khi nhân viên tạo đơn hộ khách.");
            }

            customerId = dto.CustomerId.Value;
            staffId = requestUserId;
        }
        else
        {
            throw new BusinessException("Tài khoản không hợp lệ hoặc không phải khách hàng.");
        }

        // 2. Truy vấn bảng Users kiểm tra tồn tại (404), IsActive == true và đúng Role Customer (400)
        var customer = await _userRepo.Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == customerId)
            ?? throw new NotFoundException("Không tìm thấy khách hàng.");

        bool isCustomerRole = customer.Role != null
            ? customer.Role.RoleName == AppRoles.Customer
            : customer.RoleId == CustomerRoleId;

        if (!customer.IsActive || !isCustomerRole)
        {
            throw new BusinessException("Tài khoản không hợp lệ hoặc không phải khách hàng.");
        }

        // 3. Kiểm tra chi nhánh tồn tại và đang hoạt động
        var branch = await _branchRepo.Query()
            .FirstOrDefaultAsync(b => b.BranchId == dto.BranchId && b.IsActive)
            ?? throw new NotFoundException("Chi nhánh không tồn tại hoặc đã dừng hoạt động.");

        // 4. Tính số ngày thuê (làm tròn lên, tối thiểu 1 ngày)
        int numberOfDays = (int)Math.Ceiling((dto.ExpectedReturnDate - dto.StartDate).TotalDays);
        if (numberOfDays < 1) numberOfDays = 1;

        // 5. Kiểm tra từng xe: Tồn tại, thuộc chi nhánh, sẵn sàng và không trùng lịch
        var motorbikes = await _motorbikeRepo.Query()
            .Where(m => dto.MotorbikeIds.Contains(m.MotorbikeId))
            .ToListAsync();

        if (motorbikes.Count != dto.MotorbikeIds.Count)
        {
            throw new BusinessException("Một số xe trong danh sách không tồn tại trong hệ thống.");
        }

        foreach (var bike in motorbikes)
        {
            if (bike.BranchId != dto.BranchId)
            {
                throw new BusinessException($"Xe biển số '{bike.LicensePlate}' không thuộc chi nhánh đã chọn.");
            }

            if (bike.Status == MotorbikeStatus.Inactive || bike.Status == MotorbikeStatus.Maintenance)
            {
                throw new BusinessException($"Xe biển số '{bike.LicensePlate}' đang không sẵn sàng để cho thuê ({bike.Status}).");
            }

            bool isAvailable = await _motorbikeRepo.IsAvailableAsync(bike.MotorbikeId, dto.StartDate, dto.ExpectedReturnDate);
            if (!isAvailable)
            {
                throw new ConflictException($"Xe biển số '{bike.LicensePlate}' đã có người đặt trong khoảng thời gian này.");
            }
        }

        // 6. Khởi tạo Entity Đơn thuê và Chi tiết (Snapshot giá)
        decimal subTotal = 0;
        decimal depositAmount = 0;
        var rentalDetails = new List<RentalDetail>();

        foreach (var bike in motorbikes)
        {
            subTotal += bike.PricePerDay * numberOfDays;
            depositAmount += bike.DepositAmount;

            rentalDetails.Add(new RentalDetail
            {
                MotorbikeId = bike.MotorbikeId,
                PricePerDay = bike.PricePerDay, // Snapshot giá tại thời điểm thuê
                NumberOfDays = numberOfDays,
                DepositAmount = bike.DepositAmount
            });
        }

        var rental = new Rental
        {
            RentalCode = await GenerateRentalCodeAsync(),
            CustomerId = customerId,
            StaffId = staffId,
            BranchId = dto.BranchId,
            StartDate = dto.StartDate,
            ExpectedReturnDate = dto.ExpectedReturnDate,
            Status = RentalStatus.Pending,
            SubTotal = subTotal,
            DiscountAmount = 0,
            LateFee = 0,
            DamageFee = 0,
            DepositAmount = depositAmount,
            Note = dto.Note?.Trim(),
            CreatedAt = DateTime.Now,
            RentalDetails = rentalDetails
        };

        // 7. Thực thi Transaction lưu vào DB
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Rentals.AddAsync(rental);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return (await GetDtoByIdAsync(rental.RentalId))!;
    }

    public async Task<RentalDto> ConfirmAsync(int id, ConfirmRentalDto dto, int staffId)
    {
        var rental = await GetRentalForUpdateAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy đơn thuê có Id = {id}.");

        if (rental.Status != RentalStatus.Pending)
        {
            throw new BusinessException($"Chỉ có thể duyệt đơn ở trạng thái '{RentalStatus.Pending}'. Đơn hiện tại đang '{rental.Status}'.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            rental.Status = RentalStatus.Confirmed;
            rental.StaffId = staffId;
            rental.UpdatedAt = DateTime.Now;

            if (rental.DepositAmount > 0)
            {
                var depositPayment = new Payment
                {
                    RentalId = rental.RentalId,
                    Amount = rental.DepositAmount,
                    PaymentType = PaymentType.Deposit,
                    PaymentMethod = dto.PaymentMethod,
                    Status = PaymentStatus.Paid,
                    TransactionRef = dto.TransactionRef?.Trim(),
                    PaidAt = DateTime.Now,
                    ProcessedBy = staffId,
                    Note = dto.Note?.Trim() ?? "Thu tiền cọc duyệt đơn",
                    CreatedAt = DateTime.Now
                };
                await _context.Payments.AddAsync(depositPayment);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return (await GetDtoByIdAsync(id))!;
    }

    public async Task<RentalDto> PickupAsync(int id, PickupRentalDto dto, int staffId)
    {
        var rental = await GetRentalForUpdateAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy đơn thuê có Id = {id}.");

        if (rental.Status != RentalStatus.Confirmed)
        {
            throw new BusinessException($"Chỉ có thể giao xe cho đơn ở trạng thái '{RentalStatus.Confirmed}'. Đơn hiện tại đang '{rental.Status}'.");
        }

        // BR4: Kiểm tra CCCD và GPLX của khách hàng
        if (string.IsNullOrWhiteSpace(rental.Customer.IdentityNumber) ||
            string.IsNullOrWhiteSpace(rental.Customer.DriverLicenseNumber))
        {
            throw new BusinessException("Khách hàng chưa cập nhật đầy đủ CCCD và Giấy phép lái xe trong hồ sơ cá nhân. Không đủ điều kiện bàn giao xe.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var detail in rental.RentalDetails)
            {
                var item = dto.Items.FirstOrDefault(x => x.MotorbikeId == detail.MotorbikeId)
                    ?? throw new BusinessException($"Thiếu thông tin bàn giao cho xe biển số '{detail.Motorbike.LicensePlate}'.");

                if (item.PickupOdometer < detail.Motorbike.Odometer)
                {
                    throw new BusinessException($"Số km bàn giao của xe '{detail.Motorbike.LicensePlate}' ({item.PickupOdometer} km) không được nhỏ hơn số km hiện tại ({detail.Motorbike.Odometer} km).");
                }

                detail.PickupOdometer = item.PickupOdometer;
                detail.PickupCondition = item.PickupCondition?.Trim();

                // Cập nhật trạng thái xe sang Rented
                detail.Motorbike.Status = MotorbikeStatus.Rented;
                detail.Motorbike.UpdatedAt = DateTime.Now;
            }

            rental.Status = RentalStatus.Renting;
            rental.StaffId = staffId;
            rental.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return (await GetDtoByIdAsync(id))!;
    }

    public async Task<RentalDto> ReturnAsync(int id, ReturnRentalDto dto, int staffId)
    {
        var rental = await GetRentalForUpdateAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy đơn thuê có Id = {id}.");

        if (rental.Status != RentalStatus.Renting)
        {
            throw new BusinessException($"Chỉ có thể nhận trả xe cho đơn ở trạng thái '{RentalStatus.Renting}'. Đơn hiện tại đang '{rental.Status}'.");
        }

        DateTime actualReturn = dto.ActualReturnDate ?? DateTime.Now;
        if (actualReturn < rental.StartDate)
        {
            throw new BusinessException("Thời điểm trả xe thực tế không được nhỏ hơn thời điểm nhận xe.");
        }

        if (dto.DiscountAmount > rental.SubTotal)
        {
            throw new BusinessException($"Số tiền giảm giá không được vượt quá tiền thuê ({rental.SubTotal:N0} đ).");
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Cập nhật số km và tình trạng lúc trả của từng xe
            decimal totalDayPrice = 0;
            foreach (var detail in rental.RentalDetails)
            {
                totalDayPrice += detail.PricePerDay;
                var item = dto.Items.FirstOrDefault(x => x.MotorbikeId == detail.MotorbikeId)
                    ?? throw new BusinessException($"Thiếu thông tin trả xe cho xe biển số '{detail.Motorbike.LicensePlate}'.");

                if (detail.PickupOdometer.HasValue && item.ReturnOdometer < detail.PickupOdometer.Value)
                {
                    throw new BusinessException($"Số km trả của xe '{detail.Motorbike.LicensePlate}' ({item.ReturnOdometer}) không được nhỏ hơn số km lúc nhận ({detail.PickupOdometer.Value}).");
                }

                detail.ReturnOdometer = item.ReturnOdometer;
                detail.ReturnCondition = item.ReturnCondition?.Trim();

                // Cập nhật Odometer của xe
                detail.Motorbike.Odometer = item.ReturnOdometer;
                detail.Motorbike.UpdatedAt = DateTime.Now;

                // Nếu xe bị hỏng -> chuyển sang bảo dưỡng
                detail.Motorbike.Status = item.IsDamaged
                    ? MotorbikeStatus.Maintenance
                    : MotorbikeStatus.Available;
            }

            // 2. Tính phí trễ hạn (LateFee) theo quy tắc nghiệp vụ
            decimal lateFee = 0;
            if (actualReturn > rental.ExpectedReturnDate)
            {
                double lateHours = (actualReturn - rental.ExpectedReturnDate).TotalHours;
                if (lateHours <= 1.0)
                {
                    lateFee = 0; // Trễ <= 1h: Miễn phí
                }
                else if (lateHours <= 6.0)
                {
                    // Mỗi giờ trễ tiếp theo: +10% giá thuê/ngày
                    int chargedHours = (int)Math.Ceiling(lateHours - 1.0);
                    lateFee = totalDayPrice * 0.10m * chargedHours;
                }
                else
                {
                    // Trễ > 6h: Tính thêm 1 ngày thuê trọn vẹn
                    lateFee = totalDayPrice;
                }
            }

            // 3. Cập nhật thông tin đơn thuê
            rental.ActualReturnDate = actualReturn;
            rental.LateFee = lateFee;
            rental.DamageFee = dto.DamageFee;
            rental.DiscountAmount = dto.DiscountAmount;
            rental.Status = RentalStatus.Completed;
            rental.StaffId = staffId;
            rental.UpdatedAt = DateTime.Now;

            // 4. Ghi nhận các dòng thanh toán (Payments)
            decimal rentalFeeAmount = rental.SubTotal - dto.DiscountAmount;
            if (rentalFeeAmount > 0)
            {
                await _context.Payments.AddAsync(new Payment
                {
                    RentalId = rental.RentalId,
                    Amount = rentalFeeAmount,
                    PaymentType = PaymentType.RentalFee,
                    PaymentMethod = dto.PaymentMethod,
                    Status = PaymentStatus.Paid,
                    PaidAt = DateTime.Now,
                    ProcessedBy = staffId,
                    Note = "Thanh toán tiền thuê xe khi trả xe",
                    CreatedAt = DateTime.Now
                });
            }

            decimal penaltyAmount = lateFee + dto.DamageFee;
            if (penaltyAmount > 0)
            {
                await _context.Payments.AddAsync(new Payment
                {
                    RentalId = rental.RentalId,
                    Amount = penaltyAmount,
                    PaymentType = PaymentType.Penalty,
                    PaymentMethod = dto.PaymentMethod,
                    Status = PaymentStatus.Paid,
                    PaidAt = DateTime.Now,
                    ProcessedBy = staffId,
                    Note = $"Phí phạt (Trễ hạn: {lateFee:N0} đ, Hư hỏng: {dto.DamageFee:N0} đ)",
                    CreatedAt = DateTime.Now
                });
            }

            if (rental.DepositAmount > 0)
            {
                await _context.Payments.AddAsync(new Payment
                {
                    RentalId = rental.RentalId,
                    Amount = rental.DepositAmount,
                    PaymentType = PaymentType.Refund,
                    PaymentMethod = dto.PaymentMethod,
                    Status = PaymentStatus.Paid,
                    PaidAt = DateTime.Now,
                    ProcessedBy = staffId,
                    Note = "Hoàn cọc cho khách hàng khi hoàn tất trả xe",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return (await GetDtoByIdAsync(id))!;
    }

    public async Task<RentalDto> CancelAsync(int id, CancelRentalDto dto, int requestUserId, string userRole)
    {
        var rental = await GetRentalForUpdateAsync(id)
            ?? throw new NotFoundException($"Không tìm thấy đơn thuê có Id = {id}.");

        // Customer chỉ huỷ đơn của chính mình
        if (userRole == AppRoles.Customer && rental.CustomerId != requestUserId)
        {
            throw new BusinessException("Bạn chỉ được phép huỷ đơn thuê do chính mình tạo.");
        }

        // Chỉ cho phép huỷ khi Pending hoặc Confirmed
        if (rental.Status != RentalStatus.Pending && rental.Status != RentalStatus.Confirmed)
        {
            throw new BusinessException($"Không thể huỷ đơn thuê khi đã ở trạng thái '{rental.Status}'.");
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (rental.Status == RentalStatus.Confirmed && rental.DepositAmount > 0)
            {
                await _context.Payments.AddAsync(new Payment
                {
                    RentalId = rental.RentalId,
                    Amount = rental.DepositAmount,
                    PaymentType = PaymentType.Refund,
                    PaymentMethod = dto.RefundMethod,
                    Status = PaymentStatus.Paid,
                    PaidAt = DateTime.Now,
                    ProcessedBy = (userRole != AppRoles.Customer) ? requestUserId : null,
                    Note = $"Hoàn tiền cọc do huỷ đơn: {dto.CancelReason.Trim()}",
                    CreatedAt = DateTime.Now
                });
            }

            rental.Status = RentalStatus.Cancelled;
            rental.CancelReason = dto.CancelReason.Trim();
            rental.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return (await GetDtoByIdAsync(id))!;
    }

    // ===== Helper Methods =====

    /// <summary>Đọc DTO bằng SQL Projection (AsNoTracking).</summary>
    private Task<RentalDto?> GetDtoByIdAsync(int id)
    {
        return _rentalRepo.Query()
            .Where(r => r.RentalId == id)
            .Select(RentalMappings.RentalToDto)
            .FirstOrDefaultAsync();
    }

    /// <summary>Lấy Entity có Change Tracking để cập nhật trong Transaction.</summary>
    private Task<Rental?> GetRentalForUpdateAsync(int id)
    {
        return _rentalRepo.QueryTracking()
            .Include(r => r.Customer)
            .Include(r => r.RentalDetails).ThenInclude(d => d.Motorbike)
            .FirstOrDefaultAsync(r => r.RentalId == id);
    }

    private async Task<string> GenerateRentalCodeAsync()
    {
        string datePart = DateTime.Now.ToString("yyyyMMdd");
        string prefix = $"RNT-{datePart}-";

        var lastCode = await _rentalRepo.Query()
            .Where(r => r.RentalCode.StartsWith(prefix))
            .OrderByDescending(r => r.RentalCode)
            .Select(r => r.RentalCode)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (lastCode != null && int.TryParse(lastCode.Substring(prefix.Length), out int currentSeq))
        {
            nextSeq = currentSeq + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }
}