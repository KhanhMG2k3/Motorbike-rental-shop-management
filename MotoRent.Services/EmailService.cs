using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MotoRent.Services.Interfaces;

namespace MotoRent.Services;

/// <summary>
/// Gửi email qua SMTP (vd Gmail + App Password). Cấu hình đọc từ section "Smtp" trong appsettings
/// (giá trị thật đặt ở appsettings.Local.json, không commit).
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _config["Smtp:Host"];
        var fromEmail = _config["Smtp:FromEmail"];
        var password = _config["Smtp:FromPassword"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(password))
        {
            // Chưa cấu hình Smtp (appsettings.Local.json) - log nội dung ra console để dev test được luồng
            // (quên mật khẩu, email xác nhận đơn...) mà không cần SMTP thật.
            _logger.LogWarning(
                "Chưa cấu hình Smtp - KHÔNG gửi được email tới {ToEmail}. Nội dung (chỉ hiện ở log dev):\nSubject: {Subject}\n{Body}",
                toEmail, subject, htmlBody);
            return;
        }

        try
        {
            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) || ssl;
            var fromName = _config["Smtp:FromName"] ?? "MotoRent";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(fromEmail, password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Gửi email thất bại không được làm hỏng luồng nghiệp vụ (tạo đơn, duyệt đơn, reset mật khẩu...)
            _logger.LogError(ex, "Gửi email tới {ToEmail} thất bại.", toEmail);
        }
    }
}
