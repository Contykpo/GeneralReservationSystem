using FluentValidation;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Server.Helpers;
using GeneralReservationSystem.Server.Services.Interfaces;
using GeneralReservationSystem.Web.Client.Services.Interfaces;

namespace GeneralReservationSystem.Server.Services.Implementations
{
    public class WebStationService(
        IApiStationService stationService,
        IHttpContextAccessor httpContextAccessor,
        IValidator<PagedSearchRequestDto> pagedSearchValidator,
        IValidator<CreateStationDto> createStationValidator,
        IValidator<UpdateStationDto> updateStationValidator,
        IValidator<StationKeyDto> stationKeyValidator,
        IValidator<ImportStationDto> importStationValidator) : WebServiceBase(httpContextAccessor), IClientStationService
    {
        public async Task<Station> GetStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(stationKeyValidator, keyDto, cancellationToken);
            return await stationService.GetStationAsync(keyDto, cancellationToken);
        }

        public async Task<IEnumerable<Station>> GetAllStationsAsync(CancellationToken cancellationToken = default)
        {
            return await stationService.GetAllStationsAsync(cancellationToken);
        }

        public async Task<PagedResult<Station>> SearchStationsAsync(PagedSearchRequestDto searchDto, CancellationToken cancellationToken = default)
        {
            await ValidateAsync(pagedSearchValidator, searchDto, cancellationToken);
            return await stationService.SearchStationsAsync(searchDto, cancellationToken);
        }

        public async Task<Station> CreateStationAsync(CreateStationDto dto, CancellationToken cancellationToken = default)
        {
            EnsureAuthorized();
            await ValidateAsync(createStationValidator, dto, cancellationToken);
            return await stationService.CreateStationAsync(dto, cancellationToken);
        }

        public async Task<Station> UpdateStationAsync(UpdateStationDto dto, CancellationToken cancellationToken = default)
        {
            EnsureAuthorized();
            await ValidateAsync(updateStationValidator, dto, cancellationToken);
            return await stationService.UpdateStationAsync(dto, cancellationToken);
        }

        public async Task DeleteStationAsync(StationKeyDto keyDto, CancellationToken cancellationToken = default)
        {
            EnsureAuthorized();
            await ValidateAsync(stationKeyValidator, keyDto, cancellationToken);
            await stationService.DeleteStationAsync(keyDto, cancellationToken);
        }

        public async Task<ImportResult> ImportStationsFromCsvAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default)
        {
            EnsureAuthorized();

            if (csvStream == null || !csvStream.CanRead)
            {
                throw new ServiceValidationException("El archivo CSV es requerido.", [
                    new ValidationError("El archivo CSV es requerido.", "file")
                ]);
            }

            if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new ServiceValidationException("El archivo debe ser un CSV.", [
                    new ValidationError("El archivo debe ser un CSV.", "file")
                ]);
            }

            List<ImportStationDto> importDtos = [];
            await foreach (ImportStationDto dto in CsvHelper.ParseAndValidateCsvAsync(
                csvStream,
                importStationValidator,
                cancellationToken
            ))
            {
                importDtos.Add(dto);
            }

            if (importDtos.Count == 0)
            {
                throw new ServiceValidationException("El archivo CSV no contiene estaciones válidas.", [
                    new ValidationError("El archivo CSV no contiene estaciones válidas.", "file")
                ]);
            }

            int affected = await stationService.CreateStationsBulkAsync(importDtos, cancellationToken);

            return new ImportResult($"Se importaron {affected} estaciones exitosamente.", affected);
        }

        public async Task<(byte[] FileContent, string FileName)> ExportStationsToCsvAsync(CancellationToken cancellationToken = default)
        {
            EnsureAuthorized();
            IEnumerable<Station> stations = await stationService.GetAllStationsAsync(cancellationToken);
            byte[] bytes = CsvHelper.ExportToCsv(stations);
            string fileName = $"stations_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return (bytes, fileName);
        }
    }
}
