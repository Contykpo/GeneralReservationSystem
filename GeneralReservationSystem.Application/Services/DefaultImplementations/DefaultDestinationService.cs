using System.Collections.Generic;
using System.Threading.Tasks;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class DefaultDestinationService : IDestinationService
    {
        private readonly IDestinationRepository _destinationRepository;

        public DefaultDestinationService(IDestinationRepository destinationRepository)
        {
            _destinationRepository = destinationRepository;
        }

        public async Task<OptionalResult<PagedResult<Destination>>> SearchDestinationsAsync(int pageIndex, int pageSize, string? name = null, string? code = null,
            string? city = null, string? region = null, string? country = null, GeneralReservationSystem.Application.Repositories.Interfaces.DestinationSearchSortBy? sortBy = null, bool descending = false)
        {
            return await _destinationRepository.SearchPagedAsync(pageIndex, pageSize, name, code, city, region, country, sortBy, descending);
        }

        public Task<OptionalResult<Destination>> GetDestinationByIdAsync(int id)
            => _destinationRepository.GetByIdAsync(id);

        public Task<OperationResult> AddDestinationAsync(CreateDestinationDto destinationDto)
        {
            string Normalize(string value) =>
                string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

            var destination = new Destination
            {
                Name = destinationDto.Name,
                Code = destinationDto.Code,
                City = destinationDto.City,
                Region = destinationDto.Region,
                Country = destinationDto.Country,
                NormalizedName = Normalize(destinationDto.Name),
                NormalizedCode = Normalize(destinationDto.Code),
                NormalizedCity = Normalize(destinationDto.City),
                NormalizedRegion = Normalize(destinationDto.Region),
                NormalizedCountry = Normalize(destinationDto.Country),
                TimeZone = destinationDto.TimeZone
            };
            return _destinationRepository.AddAsync(destination);
        }

        public Task<OperationResult> UpdateDestinationAsync(UpdateDestinationDto destinationDto)
        {
            string Normalize(string value) =>
                string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

            var destination = new Destination
            {
                DestinationId = destinationDto.Id,
                Name = destinationDto.Name,
                Code = destinationDto.Code,
                City = destinationDto.City,
                Region = destinationDto.Region,
                Country = destinationDto.Country,
                NormalizedName = Normalize(destinationDto.Name),
                NormalizedCode = Normalize(destinationDto.Code),
                NormalizedCity = Normalize(destinationDto.City),
                NormalizedRegion = Normalize(destinationDto.Region),
                NormalizedCountry = Normalize(destinationDto.Country),
                TimeZone = destinationDto.TimeZone
            };
            return _destinationRepository.UpdateAsync(destination);
        }

        public Task<OperationResult> DeleteDestinationAsync(int id)
            => _destinationRepository.DeleteAsync(id);
    }
}
