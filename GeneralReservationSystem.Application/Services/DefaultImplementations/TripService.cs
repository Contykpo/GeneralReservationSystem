using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class TripService(ITripRepository tripRepository) : ITripService
    {
        public async Task<Trip> CreateTripAsync(CreateTripDto dto, CancellationToken cancellationToken = default)
        {
            var trip = new Trip
            {
                DepartureStationId = dto.DepartureStationId,
                DepartureTime = dto.DepartureTime,
                ArrivalStationId = dto.ArrivalStationId,
                ArrivalTime = dto.ArrivalTime,
                AvailableSeats = dto.AvailableSeats
            };
            try
            {
                await tripRepository.CreateAsync(trip, cancellationToken);
                return trip;
            }
            catch (ForeignKeyViolationException ex)
            {
                throw new ServiceBusinessException("La estación de salida o llegada no existe.", ex);
            }
            catch (CheckConstraintViolationException ex)
            {
                if (ex.ConstraintName.Contains("CK_Trip_Departure_Arrival"))
                    throw new ServiceBusinessException("La estación de salida y llegada deben ser diferentes.", ex);
                if (ex.ConstraintName.Contains("CK_Trip_Times"))
                    throw new ServiceBusinessException("La hora de llegada debe ser posterior a la de salida.", ex);
                if (ex.ConstraintName.Contains("CK_Trip_AvailableSeats"))
                    throw new ServiceBusinessException("El número de asientos disponibles debe ser un número positivo.", ex);
                throw new ServiceBusinessException("Restricción de datos inválida en el viaje.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al crear el viaje.", ex);
            }
        }

        public async Task<Trip> UpdateTripAsync(UpdateTripDto dto, CancellationToken cancellationToken = default)
        {
            var trip = new Trip { TripId = dto.TripId };
            if (dto.DepartureStationId.HasValue)
                trip.DepartureStationId = dto.DepartureStationId.Value;
            if (dto.DepartureTime.HasValue)
                trip.DepartureTime = dto.DepartureTime.Value;
            if (dto.ArrivalStationId.HasValue)
                trip.ArrivalStationId = dto.ArrivalStationId.Value;
            if (dto.ArrivalTime.HasValue)
                trip.ArrivalTime = dto.ArrivalTime.Value;
            if (dto.AvailableSeats.HasValue)
                trip.AvailableSeats = dto.AvailableSeats.Value;

            try
            {
                var affected = await tripRepository.UpdateAsync(trip, cancellationToken: cancellationToken);
                if (affected == 0)
                    throw new ServiceNotFoundException("No se encontró el viaje para actualizar.");
                return trip;
            }
            catch (ForeignKeyViolationException ex)
            {
                throw new ServiceBusinessException("La estación de salida o llegada no existe.", ex);
            }
            catch (CheckConstraintViolationException ex)
            {
                if (ex.ConstraintName.Contains("CK_Trip_Departure_Arrival"))
                    throw new ServiceBusinessException("La estación de salida y llegada deben ser diferentes.", ex);
                if (ex.ConstraintName.Contains("CK_Trip_Times"))
                    throw new ServiceBusinessException("La hora de llegada debe ser posterior a la de salida.", ex);
                if (ex.ConstraintName.Contains("CK_Trip_AvailableSeats"))
                    throw new ServiceBusinessException("El número de asientos disponibles debe ser un número positivo.", ex);
                throw new ServiceBusinessException("Restricción de datos inválida en el viaje.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al actualizar el viaje.", ex);
            }
        }

        public async Task DeleteTripAsync(TripKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            var trip = new Trip { TripId = keyDto.TripId };
            try
            {
                var affected = await tripRepository.DeleteAsync(trip, cancellationToken);
                if (affected == 0)
                    throw new ServiceNotFoundException("No se encontró el viaje para eliminar.");
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
                var trip = await tripRepository.Query()
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

        public async Task<PagedResult<Trip>> SearchTripsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = tripRepository.Query()
                    .ApplyFilters(searchDto.Filters)
                    .ApplySorting(searchDto.Orders)
                    .Page(searchDto.Page, searchDto.PageSize);
                return await query.ToPagedResultAsync(cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar viajes.", ex);
            }
        }
    }
}
