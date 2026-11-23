using System.ComponentModel.DataAnnotations;

namespace MotoRent.BusinessObjects.DTOs.Categories;

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int MotorbikeCount { get; set; }
}

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Tên loại xe không được để trống.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Tên loại xe từ 2 đến 50 ký tự.")]
    public string CategoryName { get; set; } = null!;

    [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự.")]
    public string? Description { get; set; }
}

public class UpdateCategoryDto : CreateCategoryDto
{
    public bool IsActive { get; set; } = true;
}
