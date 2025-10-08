using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.Interfaces;
using System.Globalization;
using System.Text;

namespace GeneralReservationSystem.Application.Services.DefaultImplementations
{
    public class DestinationService : IDestinationService
    {
        private readonly IDestinationRepository _destinationRepository;

        public DestinationService(IDestinationRepository destinationRepository)
        {
            _destinationRepository = destinationRepository;
        }

        public async Task AddDestinationAsync(CreateDestinationDto destinationDto, CancellationToken cancellationToken = default)
        {
            var entity = new Destination
            {
                Name = destinationDto.Name,
                Code = destinationDto.Code,
                City = destinationDto.City,
                Region = destinationDto.Region,
                Country = destinationDto.Country,
                TimeZone = destinationDto.TimeZone,
                // No need to set normalized fields here, they will be set in the database layer as computed columns.
                /*NormalizedName = Normalize(destinationDto.Name),
                NormalizedCode = Normalize(destinationDto.Code),
                NormalizedCity = Normalize(destinationDto.City),
                NormalizedRegion = Normalize(destinationDto.Region),
                NormalizedCountry = Normalize(destinationDto.Country)*/
            };

            try
            {
                var created = await _destinationRepository.CreateAsync(entity, cancellationToken);
                if (created <= 0) throw new Exception("Failed to create the destination.");
            } catch (UniqueConstraintViolationException) {
                throw new Exception("A destination with the same name or code already exists.");
            }
        }

        public async Task DeleteDestinationAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = new Destination { DestinationId = id };
            var deleted = await _destinationRepository.DeleteAsync(entity, cancellationToken);
            if (deleted <= 0) throw new Exception("The destination to delete was not found.");
        }

        public async Task<Destination?> GetDestinationByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return null;
            return await _destinationRepository.Query()
                .Where(d => d.DestinationId == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PagedResult<Destination>> SearchDestinationsAsync(DestinationSearchRequestDto destinationSearchRequestDto, CancellationToken cancellationToken = default)
        {
            var q = _destinationRepository.Query();

            if (!string.IsNullOrWhiteSpace(destinationSearchRequestDto.Name))
            {
                var n = Normalize(destinationSearchRequestDto.Name);
                q = q.Where(d => d.NormalizedName.StartsWith(n));
            }

            if (!string.IsNullOrWhiteSpace(destinationSearchRequestDto.Code))
            {
                var n = Normalize(destinationSearchRequestDto.Code);
                q = q.Where(d => d.NormalizedCode.StartsWith(n));
            }

            if (!string.IsNullOrWhiteSpace(destinationSearchRequestDto.City))
            {
                var n = Normalize(destinationSearchRequestDto.City);
                q = q.Where(d => d.NormalizedCity.StartsWith(n));
            }

            if (!string.IsNullOrWhiteSpace(destinationSearchRequestDto.Region))
            {
                var n = Normalize(destinationSearchRequestDto.Region);
                q = q.Where(d => d.NormalizedRegion.StartsWith(n));
            }

            if (!string.IsNullOrWhiteSpace(destinationSearchRequestDto.Country))
            {
                var n = Normalize(destinationSearchRequestDto.Country);
                q = q.Where(d => d.NormalizedCountry.StartsWith(n));
            }

            if (destinationSearchRequestDto.OrderingOptions != null)
            {
                var order = destinationSearchRequestDto.OrderingOptions;
                bool ascending = order.Ascending;
                switch (order.OrderBy)
                {
                    case DestinationOrderBy.DestinationId:
                        q = q.OrderBy(d => d.DestinationId, ascending);
                        break;
                    case DestinationOrderBy.Name:
                        q = q.OrderBy(d => d.Name, ascending);
                        break;
                    case DestinationOrderBy.Code:
                        q = q.OrderBy(d => d.Code, ascending);
                        break;
                    case DestinationOrderBy.City:
                        q = q.OrderBy(d => d.City, ascending);
                        break;
                    case DestinationOrderBy.Region:
                        q = q.OrderBy(d => d.Region, ascending);
                        break;
                    case DestinationOrderBy.Country:
                        q = q.OrderBy(d => d.Country, ascending);
                        break;
                    default:
                        break;
                }
            }

            var pageNumber = destinationSearchRequestDto.PaginationOptions?.PageNumber ?? 1;
            var pageSize = destinationSearchRequestDto.PaginationOptions?.PageSize ?? 10;
            q = q.Page(pageNumber, pageSize);

            // TEMPORAL
            try
            {
                return await q.ToPagedResultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while searching for destinations: {ex.InnerException?.Message}");
                throw;
            }
        }

        public async Task UpdateDestinationAsync(UpdateDestinationDto destinationDto, CancellationToken cancellationToken = default)
        {
            var entity = new Destination
            {
                DestinationId = destinationDto.DestinationId,
                Name = destinationDto.Name,
                Code = destinationDto.Code,
                City = destinationDto.City,
                Region = destinationDto.Region,
                Country = destinationDto.Country,
                TimeZone = destinationDto.TimeZone,
                // No need to set normalized fields here, they will be set in the database layer as computed columns.
                /*NormalizedName = Normalize(destinationDto.Name),
                NormalizedCode = Normalize(destinationDto.Code),
                NormalizedCity = Normalize(destinationDto.City),
                NormalizedRegion = Normalize(destinationDto.Region),
                NormalizedCountry = Normalize(destinationDto.Country)*/
            };

            try
            {
                var updated = await _destinationRepository.UpdateAsync(entity, cancellationToken);
                if (updated <= 0) throw new Exception("The destination to update was not found.");
            } catch (UniqueConstraintViolationException) {
                throw new Exception("A destination with the same name or code already exists.");
            }
        }

        private static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var normalized = input.Trim().ToUpperInvariant();
            normalized = normalized.Normalize(NormalizationForm.FormKD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
