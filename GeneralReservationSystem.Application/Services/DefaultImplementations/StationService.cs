using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class StationService(IStationRepository stationRepository) : IStationService
    {
        public async Task<Station> CreateStationAsync(Station station, CancellationToken cancellationToken = default)
        {
            Station newStation = new()
            {
                StationName = station.StationName,
                Address = station.Address,
                City = station.City,
                Province = station.Province,
                Country = station.Country
            };
            try
            {
                _ = await stationRepository.CreateAsync(newStation, cancellationToken);
                return newStation;
            }
            catch (UniqueConstraintViolationException ex)
            {
                throw new ServiceBusinessException("Ya existe una estación con el mismo nombre o código.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al crear la estación.", ex);
            }
        }

        public async Task<Station> UpdateStationAsync(Station station, CancellationToken cancellationToken = default)
        {
            Station updatedStation = new() { StationId = station.StationId };
            if (station.StationName != null)
            {
                updatedStation.StationName = station.StationName;
            }

            if (station.Address != null)
            {
                updatedStation.Address = station.Address;
            }

            if (station.City != null)
            {
                updatedStation.City = station.City;
            }

            if (station.Province != null)
            {
                updatedStation.Province = station.Province;
            }

            if (station.Country != null)
            {
                updatedStation.Country = station.Country;
            }

            try
            {
                int affected = await stationRepository.UpdateAsync(updatedStation, cancellationToken: cancellationToken);
                return affected == 0 ? throw new ServiceNotFoundException("No se encontró la estación para actualizar.") : updatedStation;
            }
            catch (UniqueConstraintViolationException ex)
            {
                throw new ServiceBusinessException("Ya existe una estación con el mismo nombre o código.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al actualizar la estación.", ex);
            }
        }

        public async Task DeleteStationAsync(int key, CancellationToken cancellationToken = default)
        {
            Station station = new() { StationId = key };
            try
            {
                int affected = await stationRepository.DeleteAsync(station, cancellationToken);
                if (affected == 0)
                {
                    throw new ServiceNotFoundException("No se encontró la estación para eliminar.");
                }
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al eliminar la estación.", ex);
            }
        }

        public async Task<Station> GetStationAsync(int key, CancellationToken cancellationToken = default)
        {
            try
            {
                Station station = await stationRepository.Query()
                    .Where(s => s.StationId == key)
                    .FirstOrDefaultAsync(cancellationToken) ?? throw new ServiceNotFoundException("No se encontró la estación solicitada.");
                return station;
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al consultar la estación.", ex);
            }
        }

        public async Task<IEnumerable<Station>> GetAllStationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await stationRepository.GetAllAsync(cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al obtener la lista de estaciones.", ex);
            }
        }

        public async Task<PagedResult<Station>> SearchStationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            try
            {
                Repositories.Util.Interfaces.IQuery<Station> query = stationRepository.Query()
                    .ApplyFilters(searchDto.Filters)
                    .ApplySorting(searchDto.Orders)
                    .Page(searchDto.Page, searchDto.PageSize);
                return await query.ToPagedResultAsync(cancellationToken);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar estaciones.", ex);
            }
        }
    }
}
