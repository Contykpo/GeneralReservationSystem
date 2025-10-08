using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface ITripService
    {
        Task<PagedResult<TripDetailsDto>> SearchPagedAsync(TripDetailsSearchRequestDto tripDetailsSearchRequestDto, CancellationToken cancellationToken = default);
        Task<Trip?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(CreateTripDto tripDto, CancellationToken cancellationToken = default);
        Task UpdateAsync(UpdateTripDto tripDto, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
