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
    public class StationService(IStationRepository stationRepository, IUnitOfWork unitOfWork) : IStationService
    {
        public async Task<Station> CreateStationAsync(CreateStationDto dto, CancellationToken cancellationToken = default)
        {
            Station station = new()
            {
                StationName = dto.StationName,
                City = dto.City,
                Province = dto.Province,
                Country = dto.Country
            };
            try
            {
                _ = await stationRepository.CreateAsync(station, cancellationToken);
                return station;
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

        public async Task<Station> UpdateStationAsync(UpdateStationDto dto, CancellationToken cancellationToken = default)
        {
            Station station = new() { StationId = dto.StationId };
            if (dto.StationName != null)
            {
                station.StationName = dto.StationName;
            }

            if (dto.City != null)
            {
                station.City = dto.City;
            }

            if (dto.Province != null)
            {
                station.Province = dto.Province;
            }

            if (dto.Country != null)
            {
                station.Country = dto.Country;
            }

            try
            {
                int affected = await stationRepository.UpdateAsync(station, cancellationToken: cancellationToken);
                return affected == 0 ? throw new ServiceNotFoundException("No se encontró la estación para actualizar.") : station;
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

        public async Task DeleteStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            Station station = new() { StationId = keyDto.StationId };
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

        public async Task<Station> GetStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            try
            {
                Station station = await stationRepository.Query()
                    .Where(s => s.StationId == keyDto.StationId)
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
                IQueryable<Station> query = unitOfWork.StationRepository.Query()
                    .ApplyFilters(searchDto.Filters);

                List<Station> items = await query
                    .ApplySorts(searchDto.Orders)
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToListAsync(cancellationToken);

                int count = await query.CountAsync(cancellationToken);

                unitOfWork.Commit();

                return new PagedResult<Station>
                {
                    Items = items,
                    TotalCount = count,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize
                };
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al buscar estaciones.", ex);
            }
        }
    }
}
