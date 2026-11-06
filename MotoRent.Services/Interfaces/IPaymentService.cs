using MotoRent.BusinessObjects.DTOs.Payments;

namespace MotoRent.Services.Interfaces;

public interface IPaymentService
{
    /// <summary>Tra cứu thanh toán cho Admin / Staff (lọc + phân trang + tổng hợp tiền).</summary>
    Task<PaymentListDto> GetListAsync(PaymentQuery query);

    /// <summary>Lịch sử thanh toán của khách hàng đang đăng nhập (mọi đơn của mình).</summary>
    Task<List<PaymentDto>> GetMyAsync(int customerId);

    /// <summary>Xác nhận khoản đang chờ (Pending) đã thu tiền → Paid.</summary>
    Task<PaymentDto> ConfirmAsync(int id, ConfirmPaymentDto dto, int staffId);

    /// <summary>Huỷ khoản đang chờ (Pending) → Cancelled, ghi lý do.</summary>
    Task<PaymentDto> CancelAsync(int id, CancelPaymentDto dto, int staffId);
}
