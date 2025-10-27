using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Web.Services.Interfaces
{
    public interface IClientReservationService : IReservationService
    {
        Task<IEnumerable<Reservation>> GetCurrentUserReservationsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Reservation>> GetCurrentUserReservationsForTripAsync(int tripId, CancellationToken cancellationToken = default);
        Task<Reservation> CreateCurrentUserReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default);
    }
}
