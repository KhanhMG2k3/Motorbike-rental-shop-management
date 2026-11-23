using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MotoRent.BusinessObjects.DTOs.Rentals;

namespace MotoRent.Services.Interfaces
{
    public interface IRentalService
    {
        Task<RentalDto> GetByIdAsync(int id, int? currentUserId = null, string? currentRole = null);
        Task<List<RentalDto>> GetMyRentalsAsync(int customerId);
        Task<RentalDto> CreateAsync(CreateRentalDto dto, int requestUserId, string userRole);
        Task<RentalDto> ConfirmAsync(int id, ConfirmRentalDto dto, int staffId);
        Task<RentalDto> PickupAsync(int id, PickupRentalDto dto, int staffId);
        Task<RentalDto> ReturnAsync(int id, ReturnRentalDto dto, int staffId);
        Task<RentalDto> CancelAsync(int id, CancelRentalDto dto, int requestUserId, string userRole);
    }
}
