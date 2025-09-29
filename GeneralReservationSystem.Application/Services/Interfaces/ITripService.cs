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
        Task<OptionalResult<IList<TripDetailsDto>>> SearchPagedAsync(int pageIndex, int pageSize, string? departureName = null, string? departureCity = null, string? destinationName = null, string? destinationCity = null, DateTime? startDate = null, DateTime? endDate = null, bool onlyWithAvailableSeats = true, TripSearchSortBy? sortBy = null, bool descending = false);
        Task<OptionalResult<Trip>> GetByIdAsync(int id);
        Task<OperationResult> AddAsync(CreateTripDto tripDto);
        Task<OperationResult> UpdateAsync(Trip trip);
        Task<OperationResult> DeleteAsync(int id);
    }
}
