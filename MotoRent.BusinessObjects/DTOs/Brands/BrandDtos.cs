using System.ComponentModel.DataAnnotations;

namespace MotoRent.BusinessObjects.DTOs.Brands;

public class BrandDto
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = null!;
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public int MotorbikeCount { get; set; }
}

public class CreateBrandDto
{
    [Required(ErrorMessage = "Tên hãng không được để trống.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Tên hãng từ 2 đến 50 ký tự.")]
    public string BrandName { get; set; } = null!;

    [StringLength(50, ErrorMessage = "Quốc gia tối đa 50 ký tự.")]
    public string? Country { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn logo tối đa 500 ký tự.")]
    public string? LogoUrl { get; set; }
}

public class UpdateBrandDto : CreateBrandDto
{
    public bool IsActive { get; set; } = true;
}
