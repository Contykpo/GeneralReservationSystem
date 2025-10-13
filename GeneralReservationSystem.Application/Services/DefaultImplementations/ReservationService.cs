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
        public async Task CreateReservationAsync(CreateReservationDto dto, int userId, CancellationToken cancellationToken = default)
        {
            var reservation = new Reservation
            {
                TripId = dto.TripId,
                UserId = userId,
                Seat = dto.Seat
            };
            try
            {
                await reservationRepository.CreateAsync(reservation, cancellationToken);
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

        public async Task DeleteReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            var reservation = new Reservation
            {
                TripId = keyDto.TripId,
                UserId = keyDto.UserId
            };
            try
            {
                var affected = await reservationRepository.DeleteAsync(reservation, cancellationToken);
                if (affected == 0)
                    throw new ServiceNotFoundException("No se encontró la reserva para eliminar.");
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al eliminar la reserva.", ex);
            }
        }

        public async Task<Reservation> GetReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var reservation = await reservationRepository.Query()
                    .Where(r => r.TripId == keyDto.TripId && r.UserId == keyDto.UserId)
                    .FirstOrDefaultAsync(cancellationToken);
                return reservation ?? throw new ServiceNotFoundException("No se encontró la reserva solicitada.");
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
                var query = reservationRepository.Query()
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
