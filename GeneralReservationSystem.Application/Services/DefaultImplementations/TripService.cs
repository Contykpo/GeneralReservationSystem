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
    public class TripService(ITripRepository tripRepository, IUnitOfWork unitOfWork) : ITripService
    {
        public async Task<Trip> CreateTripAsync(CreateTripDto dto, CancellationToken cancellationToken = default)
        {
            Trip trip = new()
            {
                DepartureStationId = dto.DepartureStationId,
                DepartureTime = dto.DepartureTime,
                ArrivalStationId = dto.ArrivalStationId,
                ArrivalTime = dto.ArrivalTime,
                AvailableSeats = dto.AvailableSeats
            };
            try
            {
                _ = await tripRepository.CreateAsync(trip, cancellationToken);
                return trip;
            }
            catch (ForeignKeyViolationException ex)
            {
                throw new ServiceBusinessException("La estación de salida o llegada no existe.", ex);
            }
            catch (CheckConstraintViolationException ex)
            {
                if (ex.ConstraintName.Contains("CK_Trip_Departure_Arrival"))
                {
                    throw new ServiceBusinessException("La estación de salida y llegada deben ser diferentes.", ex);
                }

                if (ex.ConstraintName.Contains("CK_Trip_Times"))
                {
                    throw new ServiceBusinessException("La hora de llegada debe ser posterior a la de salida.", ex);
                }

                if (ex.ConstraintName.Contains("CK_Trip_AvailableSeats"))
                {
                    throw new ServiceBusinessException("El número de asientos disponibles debe ser un número positivo.", ex);
                }

                throw new ServiceBusinessException("Restricción de datos inválida en el viaje.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al crear el viaje.", ex);
            }
        }

        public async Task<Trip> UpdateTripAsync(UpdateTripDto dto, CancellationToken cancellationToken = default)
        {
            Trip trip = new() { TripId = dto.TripId };
            if (dto.DepartureStationId.HasValue)
            {
                trip.DepartureStationId = dto.DepartureStationId.Value;
            }

            if (dto.DepartureTime.HasValue)
            {
                trip.DepartureTime = dto.DepartureTime.Value;
            }

            if (dto.ArrivalStationId.HasValue)
            {
                trip.ArrivalStationId = dto.ArrivalStationId.Value;
            }

            if (dto.ArrivalTime.HasValue)
            {
                trip.ArrivalTime = dto.ArrivalTime.Value;
            }

            if (dto.AvailableSeats.HasValue)
            {
                trip.AvailableSeats = dto.AvailableSeats.Value;
            }

            try
            {
                int affected = await tripRepository.UpdateAsync(trip, cancellationToken: cancellationToken);
                return affected == 0 ? throw new ServiceNotFoundException("No se encontró el viaje para actualizar.") : trip;
            }
            catch (ForeignKeyViolationException ex)
            {
                throw new ServiceBusinessException("La estación de salida o llegada no existe.", ex);
            }
            catch (CheckConstraintViolationException ex)
            {
                if (ex.ConstraintName.Contains("CK_Trip_Departure_Arrival"))
                {
                    throw new ServiceBusinessException("La estación de salida y llegada deben ser diferentes.", ex);
                }

                if (ex.ConstraintName.Contains("CK_Trip_Times"))
                {
                    throw new ServiceBusinessException("La hora de llegada debe ser posterior a la de salida.", ex);
                }

                if (ex.ConstraintName.Contains("CK_Trip_AvailableSeats"))
                {
                    throw new ServiceBusinessException("El número de asientos disponibles debe ser un número positivo.", ex);
                }

                throw new ServiceBusinessException("Restricción de datos inválida en el viaje.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al actualizar el viaje.", ex);
            }
        }

        public async Task DeleteTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            Trip trip = new() { TripId = keyDto.TripId };
            try
            {
                int affected = await tripRepository.DeleteAsync(trip, cancellationToken);
                if (affected == 0)
                {
                    throw new ServiceNotFoundException("No se encontró el viaje para eliminar.");
                }
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al eliminar el viaje.", ex);
            }
        }

        public async Task<Trip> GetTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            try
            {
                Trip trip = await tripRepository.Query()
                    .Where(t => t.TripId == keyDto.TripId)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró el viaje solicitado.");
                return trip;
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al consultar el viaje.", ex);
            }
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await tripRepository.GetAllAsync(cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al obtener la lista de viajes.", ex);
            }
        }

        public async Task<PagedResult<TripWithDetailsDto>> SearchTripsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = unitOfWork.TripRepository.Query()
                    .Join(unitOfWork.StationRepository.Query(),
                        trip => trip.DepartureStationId,
                        departureStation => departureStation.StationId,
                        (trip, departureStation) => new { trip, departureStation })
                    .Join(unitOfWork.StationRepository.Query(),
                        trip => trip.trip.ArrivalStationId,
                        arrivalStation => arrivalStation.StationId,
                        (trip, arrivalStation) => new { trip, arrivalStation })
                    .Select(x => new
                    {
                        TripId = x.trip.trip.TripId,
                        DepartureStationId = x.trip.trip.DepartureStationId,
                        DepartureStationName = x.trip.departureStation.StationName,
                        DepartureCity = x.trip.departureStation.City,
                        DepartureRegion = x.trip.departureStation.Region,
                        DepartureCountry = x.trip.departureStation.Country,
                        DepartureTime = x.trip.trip.DepartureTime,
                        ArrivalStationId = x.arrivalStation.StationId,
                        ArrivalStationName = x.arrivalStation.StationName,
                        ArrivalCity = x.arrivalStation.City,
                        ArrivalRegion = x.arrivalStation.Region,
                        ArrivalCountry = x.arrivalStation.Country,
                        ArrivalTime = x.trip.trip.ArrivalTime,
                        AvailableSeats = x.trip.trip.AvailableSeats,
                        ReservedSeats = unitOfWork.ReservationRepository.Query().Count(r => r.TripId == x.trip.trip.TripId)
                    })
                    .ApplyFilters(searchDto.Filters);

                var items = await query
                    .ApplySorts(searchDto.Orders)
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToListAsync(cancellationToken);

                List<TripWithDetailsDto> castedItems = [.. items
                    .Select(x => new TripWithDetailsDto
                    {
                        TripId = x.TripId,
                        DepartureStationId = x.DepartureStationId,
                        DepartureStationName = x.DepartureStationName,
                        DepartureCity = x.DepartureCity,
                        DepartureRegion = x.DepartureRegion,
                        DepartureCountry = x.DepartureCountry,
                        DepartureTime = x.DepartureTime,
                        ArrivalStationId = x.ArrivalStationId,
                        ArrivalStationName = x.ArrivalStationName,
                        ArrivalCity = x.ArrivalCity,
                        ArrivalRegion = x.ArrivalRegion,
                        ArrivalCountry = x.ArrivalCountry,
                        ArrivalTime = x.ArrivalTime,
                        AvailableSeats = x.AvailableSeats,
                        ReservedSeats = x.ReservedSeats
                    })];

                int count = await query.CountAsync(cancellationToken);

                unitOfWork.Commit();

                return new PagedResult<TripWithDetailsDto>
                {
                    Items = castedItems,
                    TotalCount = count,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize
                };
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar viajes.", ex);
            }
        }
    }
}
