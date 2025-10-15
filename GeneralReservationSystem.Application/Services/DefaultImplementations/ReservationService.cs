using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class ReservationService(IReservationRepository reservationRepository) : IReservationService
    {
        public async Task CreateReservationAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            Reservation newReservation = new()
            {
                TripId = reservation.TripId,
                UserId = reservation.UserId,
                Seat = reservation.Seat
            };
            try
            {
                _ = await reservationRepository.CreateAsync(newReservation, cancellationToken);
            }
            catch (ForeignKeyViolationException ex)
            {
                throw new ServiceBusinessException("El viaje o el usuario no existen.", ex);
            }
            catch (UniqueConstraintViolationException ex)
            {
                throw new ServiceBusinessException("El asiento ya está reservado para este viaje.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al crear la reserva.", ex);
            }
        }

        public async Task DeleteReservationAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            Reservation deleteReservation = new()
            {
                TripId = reservation.TripId,
                UserId = reservation.UserId
            };
            try
            {
                int affected = await reservationRepository.DeleteAsync(deleteReservation, cancellationToken);
                if (affected == 0)
                {
                    throw new ServiceNotFoundException("No se encontró la reserva para eliminar.");
                }
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al eliminar la reserva.", ex);
            }
        }

        public async Task<Reservation> GetReservationAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            try
            {
                Reservation? currentReservation = await reservationRepository.Query()
                    .Where(r => r.TripId == reservation.TripId && r.UserId == reservation.UserId)
                    .FirstOrDefaultAsync(cancellationToken);
                return currentReservation ?? throw new ServiceNotFoundException("No se encontró la reserva solicitada.");
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al consultar la reserva.", ex);
            }
        }

        public async Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await reservationRepository.Query()
                    .Where(r => r.UserId == userId)
                    .ToListAsync(cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al obtener las reservas del usuario.", ex);
            }
        }

        public async Task<PagedResult<Reservation>> SearchReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            try
            {
                Repositories.Util.Interfaces.IQuery<Reservation> query = reservationRepository.Query()
                    .ApplyFilters(searchDto.Filters)
                    .ApplySorting(searchDto.Orders)
                    .Page(searchDto.Page, searchDto.PageSize);
                return await query.ToPagedResultAsync(cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar reservas.", ex);
            }
        }
    }
}
