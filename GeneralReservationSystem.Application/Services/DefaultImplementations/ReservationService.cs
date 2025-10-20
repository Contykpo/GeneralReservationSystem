using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Util.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class ReservationService(IReservationRepository reservationRepository, IUnitOfWork unitOfWork) : IReservationService
    {
        public async Task CreateReservationAsync(CreateReservationDto dto, int userId, CancellationToken cancellationToken = default)
        {
            Reservation reservation = new()
            {
                TripId = dto.TripId,
                UserId = userId,
                Seat = dto.Seat
            };
            try
            {
                _ = await reservationRepository.CreateAsync(reservation, cancellationToken);
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
            Reservation reservation = new()
            {
                TripId = keyDto.TripId,
                UserId = keyDto.UserId
            };
            try
            {
                int affected = await reservationRepository.DeleteAsync(reservation, cancellationToken);
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

        public async Task<Reservation> GetReservationAsync(ReservationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            try
            {
                Reservation reservation = await reservationRepository.Query()
                    .Where(r => r.TripId == keyDto.TripId && r.UserId == keyDto.UserId)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró la reserva solicitada.");

                return reservation;
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
                var query = unitOfWork.ReservationRepository.Query()
                    .ApplyFilters(searchDto.Filters);

                var items = await query
                    .ApplySorts(searchDto.Orders)
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToListAsync(cancellationToken);

                int count = await query.CountAsync(cancellationToken);

                unitOfWork.Commit();

                return new PagedResult<Reservation>
                {
                    Items = items,
                    TotalCount = count,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize
                };
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar reservas.", ex);
            }
        }
    }
}
