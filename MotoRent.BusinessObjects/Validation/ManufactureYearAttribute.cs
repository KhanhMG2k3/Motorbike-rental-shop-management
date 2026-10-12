using System.ComponentModel.DataAnnotations;

namespace MotoRent.BusinessObjects.Validation;

/// <summary>
/// Năm sản xuất từ 2000 đến năm hiện tại + 1.
/// </summary>
public class ManufactureYearAttribute : ValidationAttribute
{
    public const int MinYear = 2000;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success;

        var maxYear = DateTime.Now.Year + 1;
        if (value is int year && year >= MinYear && year <= maxYear)
            return ValidationResult.Success;

        return new ValidationResult($"Năm sản xuất phải từ {MinYear} đến {maxYear}.");
    }
}
