using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MotoRent.BusinessObjects.Constants;
using MotoRent.BusinessObjects.DTOs.Payments;
using MotoRent.BusinessObjects.Entities;
using MotoRent.BusinessObjects.Exceptions;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;

namespace MotoRent.Services;

/// <summary>
/// Quản lý / tra cứu thanh toán. Các khoản thanh toán được tạo tự động trong luồng thuê xe
/// (RentalService: cọc, tiền thuê, phạt, hoàn cọc). Service này phục vụ xem, lọc, và xử lý các khoản
/// đang chờ (VD: khách thanh toán online VNPay / Momo cần Staff xác nhận).
/// </summary>
public class PaymentService : IPaymentService
{
    private static readonly Expression<Func<Payment, PaymentDto>> ToDto = p => new PaymentDto
    {
        PaymentId = p.PaymentId,
        RentalId = p.RentalId,
        RentalCode = p.Rental.RentalCode,
        CustomerName = p.Rental.Customer.FullName,
        Amount = p.Amount,
        PaymentType = p.PaymentType,
        PaymentMethod = p.PaymentMethod,
        Status = p.Status,
        TransactionRef = p.TransactionRef,
        PaidAt = p.PaidAt,
        ProcessedBy = p.ProcessedBy,
        ProcessedByName = p.ProcessedByNavigation != null ? p.ProcessedByNavigation.FullName : null,
        Note = p.Note,
        CreatedAt = p.CreatedAt
    };

    private readonly IGenericRepository<Payment> _paymentRepo;

    public PaymentService(IGenericRepository<Payment> paymentRepo)
    {
        _paymentRepo = paymentRepo;
    }

    public async Task<PaymentListDto> GetListAsync(PaymentQuery query)
    {
        var q = _paymentRepo.Query();

        if (query.RentalId.HasValue) q = q.Where(p => p.RentalId == query.RentalId);
        if (!string.IsNullOrEmpty(query.Type)) q = q.Where(p => p.PaymentType == query.Type);
        if (!string.IsNullOrEmpty(query.Method)) q = q.Where(p => p.PaymentMethod == query.Method);
        if (!string.IsNullOrEmpty(query.Status)) q = q.Where(p => p.Status == query.Status);

        // Lọc theo ngày thanh toán (chưa thanh toán thì lấy ngày tạo)
        if (query.From.HasValue)
        {
            var from = query.From.Value.Date;
            q = q.Where(p => (p.PaidAt ?? p.CreatedAt) >= from);
        }
        if (query.To.HasValue)
        {
            var toExclusive = query.To.Value.Date.AddDays(1);
            q = q.Where(p => (p.PaidAt ?? p.CreatedAt) < toExclusive);
        }

        var total = await q.CountAsync();

        // Tổng tiền chỉ tính các khoản ĐÃ thanh toán
        var sums = await q.Where(p => p.Status == PaymentStatus.Paid)
            .GroupBy(p => p.PaymentType)
            .Select(g => new { Type = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();
        decimal Sum(string type) => sums.FirstOrDefault(s => s.Type == type)?.Total ?? 0;

        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.PaymentId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToDto)
            .ToListAsync();

        return new PaymentListDto
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCollected = Sum(PaymentType.Deposit) + Sum(PaymentType.RentalFee) + Sum(PaymentType.Penalty),
            TotalRefunded = Sum(PaymentType.Refund),
            Revenue = Sum(PaymentType.RentalFee) + Sum(PaymentType.Penalty)
        };
    }

    public Task<List<PaymentDto>> GetMyAsync(int customerId)
    {
        return _paymentRepo.Query()
            .Where(p => p.Rental.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.PaymentId)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<PaymentDto> ConfirmAsync(int id, ConfirmPaymentDto dto, int staffId)
    {
        var payment = await FindPendingAsync(id, "xác nhận");

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.Now;
        payment.ProcessedBy = staffId;
        if (!string.IsNullOrWhiteSpace(dto.TransactionRef)) payment.TransactionRef = dto.TransactionRef.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Note)) payment.Note = dto.Note.Trim();

        await _paymentRepo.SaveChangesAsync();
        return await GetDtoAsync(id);
    }

    public async Task<PaymentDto> CancelAsync(int id, CancelPaymentDto dto, int staffId)
    {
        var payment = await FindPendingAsync(id, "huỷ");

        payment.Status = PaymentStatus.Cancelled;
        payment.ProcessedBy = staffId;
        payment.Note = $"Đã huỷ: {dto.Reason.Trim()}";

        await _paymentRepo.SaveChangesAsync();
        return await GetDtoAsync(id);
    }

    /// <summary>Lấy khoản thanh toán còn Pending (có tracking). Khoản đã xử lý hoặc thuộc đơn đã huỷ thì không thao tác được.</summary>
    private async Task<Payment> FindPendingAsync(int id, string action)
    {
        var payment = await _paymentRepo.QueryTracking()
            .Include(p => p.Rental)
            .FirstOrDefaultAsync(p => p.PaymentId == id)
            ?? throw new NotFoundException($"Không tìm thấy khoản thanh toán có Id = {id}.");

        if (payment.Status != PaymentStatus.Pending)
            throw new BusinessException($"Chỉ {action} được khoản thanh toán đang chờ (Pending). Khoản này đang ở trạng thái '{payment.Status}'.");

        if (action == "xác nhận" && payment.Rental.Status == RentalStatus.Cancelled)
            throw new BusinessException("Đơn thuê của khoản thanh toán này đã bị huỷ, không thể xác nhận thu tiền.");

        return payment;
    }

    private async Task<PaymentDto> GetDtoAsync(int id)
        => await _paymentRepo.Query().Where(p => p.PaymentId == id).Select(ToDto).FirstAsync();
}
