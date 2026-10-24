namespace MotoRent.BusinessObjects.DTOs.Branches;

/// <summary>
/// Thông tin chi nhánh rút gọn — dùng cho dropdown (chọn chi nhánh của xe).
/// CRUD chi nhánh đầy đủ thuộc module Branches (Dev 1).
/// </summary>
public class BranchLookupDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Address { get; set; } = null!;
}
