using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.DefaultImplementations;
using GeneralReservationSystem.Server.Services.Interfaces;

namespace GeneralReservationSystem.Server.Services.Implementations
{
    public class ApiStationService(IStationRepository stationRepository) : StationService(stationRepository), IApiStationService
    {
        private readonly IStationRepository stationRepository = stationRepository;

        public async Task<int> CreateStationsBulkAsync(IEnumerable<ImportStationDto> importDtos, CancellationToken cancellationToken = default)
        {
            try
            {
                List<Station> stations = [.. importDtos.Select(dto => new Station
                {
                    StationName = dto.StationName,
                    City = dto.City,
                    Province = dto.Province,
                    Country = dto.Country
                })];

                return await stationRepository.CreateBulkAsync(stations, cancellationToken);
            }
            catch (UniqueConstraintViolationException ex)
            {
                throw new ServiceDuplicateException("Una o más estaciones tienen nombres duplicados/ya registrados.", ex);
            }
            catch (RepositoryException ex)
            {
                throw new ServiceException("Error al crear las estaciones en lote.", ex);
            }
        }
    }
}
