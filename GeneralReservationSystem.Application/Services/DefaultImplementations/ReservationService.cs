using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class ReservationService(IReservationRepository reservationRepository) : IReservationService
    {
        public async Task<Reservation> CreateReservationAsync(CreateReservationDto dto, CancellationToken cancellationToken = default)
        {
            Reservation reservation = new()
            {
                TripId = dto.TripId,
                UserId = dto.UserId,
                Seat = dto.Seat
            };
            try
            {
                _ = await reservationRepository.CreateAsync(reservation, cancellationToken);
                return reservation;
            }
            catch (ForeignKeyViolationException ex)
            {
                throw new ServiceReferenceException("El viaje o el usuario no existen.", ex);
            }
            catch (UniqueConstraintViolationException ex)
            {
                throw new ServiceDuplicateException("El asiento ya está reservado para este viaje.", ex);
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
                Seat = keyDto.Seat
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
                return await reservationRepository.GetByKeyAsync(keyDto.TripId, keyDto.Seat, cancellationToken) ?? throw new ServiceNotFoundException("No se encontró la reserva solicitada.");
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al consultar la reserva.", ex);
            }
        }

        public async Task<IEnumerable<UserReservationDetailsDto>> GetUserReservationsAsync(UserKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            try
            {

                return await reservationRepository.GetByUserIdWithDetailsAsync(keyDto.UserId, cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al obtener las reservas del usuario.", ex);
            }
        }

        public async Task<PagedResult<ReservationDetailsDto>> SearchReservationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            try
            {
                return await reservationRepository.SearchWithDetailsAsync(searchDto, cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar reservas.", ex);
            }
        }

        public async Task<PagedResult<UserReservationDetailsDto>> SearchUserReservationsAsync(UserKeyDto keyDto, PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            try
            {
                /*var query = unitOfWork.ReservationRepository.Query()
                    .Join(unitOfWork.TripRepository.Query(),
                        r => r.TripId,
                        t => t.TripId,
                        (r, t) => new { r, t })
                    .Join(unitOfWork.StationRepository.Query(),
                        x => x.t.DepartureStationId,
                        dst => dst.StationId,
                        (x, dst) => new { x.r, x.t, dst })
                    .Join(unitOfWork.StationRepository.Query(),
                        x => x.t.ArrivalStationId,
                        ast => ast.StationId,
                        (x, ast) => new { x.r, x.t, x.dst, ast })
                    .Where(x => x.r.UserId == keyDto.UserId)
                    .Select(x => new
                    {
                        x.t.TripId,
                        x.t.DepartureStationId,
                        DepartureStationName = x.dst.StationName,
                        DepartureCity = x.dst.City,
                        DepartureProvince = x.dst.Province,
                        DepartureCountry = x.dst.Country,
                        x.t.DepartureTime,
                        ArrivalStationId = x.ast.StationId,
                        ArrivalStationName = x.ast.StationName,
                        ArrivalCity = x.ast.City,
                        ArrivalProvince = x.ast.Province,
                        ArrivalCountry = x.ast.Country,
                        x.t.ArrivalTime,
                        x.r.Seat
                    })
                    .ApplyFilters(searchDto.Filters); // TODO: This may override the user filter. May need an alternative approach to filters.

                var items = await query
                    .ApplyOrders(searchDto.Orders)
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToListAsync(cancellationToken);

                List<UserReservationDetailsDto> castedItems = [.. items
                    .Select(x => new UserReservationDetailsDto
                    {
                        TripId = x.TripId,
                        DepartureStationId = x.DepartureStationId,
                        DepartureStationName = x.DepartureStationName,
                        DepartureCity = x.DepartureCity,
                        DepartureProvince = x.DepartureProvince,
                        DepartureCountry = x.DepartureCountry,
                        DepartureTime = x.DepartureTime,
                        ArrivalStationId = x.ArrivalStationId,
                        ArrivalStationName = x.ArrivalStationName,
                        ArrivalCity = x.ArrivalCity,
                        ArrivalProvince = x.ArrivalProvince,
                        ArrivalCountry = x.ArrivalCountry,
                        ArrivalTime = x.ArrivalTime,
                        Seat = x.Seat
                    })];

                int count = await query.CountAsync(cancellationToken);

                unitOfWork.Commit();

                return new PagedResult<UserReservationDetailsDto>
                {
                    Items = castedItems,
                    TotalCount = count,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize
                };*/

                return await reservationRepository.SearchForUserIdWithDetailsAsync(keyDto.UserId, searchDto, cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar reservas.", ex);
            }
        }
    }
}
