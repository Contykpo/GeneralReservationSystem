using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Web.Services.Interfaces
{
    public interface IClientReservationService : IReservationService
    {
        Task<IEnumerable<Reservation>> GetCurrentUserReservationsAsync(CancellationToken cancellationToken = default);
    }
}
