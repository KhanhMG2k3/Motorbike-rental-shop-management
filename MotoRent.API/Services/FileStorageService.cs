using MotoRent.BusinessObjects.Exceptions;

namespace MotoRent.API.Services;

public interface IFileStorageService
{
    /// <summary>Lưu ảnh vào wwwroot/uploads/{folder}, trả về đường dẫn tương đối (VD: /uploads/motorbikes/abc.jpg).</summary>
    Task<string> SaveImageAsync(IFormFile file, string folder);

    /// <summary>Xoá file đã upload (bỏ qua ảnh không nằm trong /uploads/).</summary>
    void DeleteIfUploaded(string? relativeUrl);
}

/// <summary>
/// Lưu file ảnh trên ổ đĩa của API. Thư mục wwwroot/uploads không đưa lên Git.
/// </summary>
public class FileStorageService : IFileStorageService
{
    public const long MaxImageBytes = 2 * 1024 * 1024; // 2 MB
    private const string UploadRoot = "uploads";

    private static readonly Dictionary<string, byte[][]> AllowedImages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Phần mở rộng → chữ ký đầu file (magic bytes) để chặn file giả đuôi ảnh
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        [".webp"] = new[] { "RIFF"u8.ToArray() }
    };

    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        if (file.Length == 0)
            throw new BusinessException("File ảnh rỗng.");
        if (file.Length > MaxImageBytes)
            throw new BusinessException("Ảnh tối đa 2 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImages.TryGetValue(extension, out var signatures))
            throw new BusinessException("Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .webp.");

        await using (var check = file.OpenReadStream())
        {
            var header = new byte[8];
            var read = await check.ReadAsync(header);
            if (!signatures.Any(sig => read >= sig.Length && header.AsSpan(0, sig.Length).SequenceEqual(sig)))
                throw new BusinessException("Nội dung file không phải ảnh hợp lệ.");
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var directory = Path.Combine(webRoot, UploadRoot, folder);
        Directory.CreateDirectory(directory);

        // Tên file ngẫu nhiên → không bị trùng, không dùng tên do người dùng gửi lên
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using (var stream = File.Create(Path.Combine(directory, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        return $"/{UploadRoot}/{folder}/{fileName}";
    }

    public void DeleteIfUploaded(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl) || !relativeUrl.StartsWith($"/{UploadRoot}/"))
            return;

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativeUrl.TrimStart('/')));

        // Chỉ xoá file nằm trong thư mục uploads
        var uploadDir = Path.GetFullPath(Path.Combine(webRoot, UploadRoot));
        if (fullPath.StartsWith(uploadDir, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
