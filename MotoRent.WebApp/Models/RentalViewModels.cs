using MotoRent.BusinessObjects.DTOs.Motorbikes;
using MotoRent.BusinessObjects.DTOs.Rentals;
using MotoRent.BusinessObjects.DTOs.Reviews;

namespace MotoRent.WebApp.Models;

/// <summary>ViewModel trang Đặt thuê xe của Customer.</summary>
public class CustomerCreateRentalViewModel
{
    public int PrimaryMotorbikeId { get; set; }
    public MotorbikeDto SelectedMotorbike { get; set; } = null!;
    public List<MotorbikeDto> BranchMotorbikes { get; set; } = new();
    public CreateRentalDto Form { get; set; } = new();
    public bool MissingDrivingDocs { get; set; }
}

/// <summary>ViewModel trang Đơn thuê của tôi (My Rentals).</summary>
public class MyRentalsViewModel
{
    public List<RentalDto> AllRentals { get; set; } = new();
    public List<RentalDto> FilteredRentals { get; set; } = new();
    public string? StatusFilter { get; set; }
    public string? ErrorMessage { get; set; }

    public int CountByStatus(string? status) => string.IsNullOrEmpty(status)
        ? AllRentals.Count
        : AllRentals.Count(r => r.Status == status);
}

/// <summary>ViewModel trang Chi tiết đơn thuê & Theo dõi tiến độ của Customer.</summary>
public class CustomerRentalDetailViewModel
{
    public RentalDto Rental { get; set; } = null!;
    public List<ReviewDto> Reviews { get; set; } = new();
    public CancelRentalDto CancelForm { get; set; } = new();
}