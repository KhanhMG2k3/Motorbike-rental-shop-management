using System.ComponentModel.DataAnnotations;

namespace MotoRent.API.Models;

/// <summary>Form upload ảnh (multipart/form-data).</summary>
public class UploadImageRequest
{
    [Required(ErrorMessage = "Vui lòng chọn file ảnh.")]
    public IFormFile File { get; set; } = null!;
}
