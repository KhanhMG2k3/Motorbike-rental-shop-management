namespace MotoRent.Services.Interfaces;

/// <summary>Gửi email (SMTP). Lỗi gửi mail không được làm hỏng luồng nghiệp vụ gọi nó.</summary>
public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
