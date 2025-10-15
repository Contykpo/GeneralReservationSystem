using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Moq;

using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Common;

namespace GeneralReservationSystem.MockServices
{
	public static class MockTripService
	{
		public static readonly Dictionary<int, Trip> Trips = new()
		{
			{1,  new Trip { TripId = 1, DepartureStationId = 1, ArrivalStationId = 2, AvailableSeats = 20 } },
			{2,  new Trip { TripId = 2, DepartureStationId = 2, ArrivalStationId = 3, AvailableSeats = 20 } },
			{3,  new Trip { TripId = 3, DepartureStationId = 3, ArrivalStationId = 4, AvailableSeats = 20 } },
		};

		public static ITripService GetService()
		{
			var mock = new Mock<ITripService>();
			 
			mock.Setup(service => service.GetAllTripsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(Trips.Values);

			mock.Setup(service => service.GetTripAsync(It.IsAny<TripKeyDto>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((TripKeyDto tripDto, CancellationToken _) =>
				{
					if(!Trips.TryGetValue(tripDto.TripId, out var foundTrip))
						throw new ServiceNotFoundException($"Trip with ID {tripDto.TripId} not found.");

					return foundTrip;
				});

			mock.Setup(service => service.CreateTripAsync(It.IsAny<CreateTripDto>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((CreateTripDto createDto, CancellationToken _) =>
				{
					var newTripId = Trips.Keys.Max() + 1;

					var newTrip = new Trip
					{
						TripId				= newTripId,
						DepartureStationId	= createDto.DepartureStationId,
						DepartureTime		= createDto.DepartureTime,
						ArrivalStationId	= createDto.ArrivalStationId,
						ArrivalTime			= createDto.ArrivalTime,
						AvailableSeats		= createDto.AvailableSeats
					};

					Trips.Add(newTrip.TripId, newTrip);

					return newTrip;
				});

			mock.Setup(service => service.UpdateTripAsync(It.IsAny<UpdateTripDto>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((UpdateTripDto updateDto, CancellationToken _) =>
				{
					if (!Trips.TryGetValue(updateDto.TripId, out var foundTrip))
						throw new ServiceNotFoundException($"Trip with ID {updateDto.TripId} not found.");

					if (updateDto.DepartureStationId.HasValue)
						foundTrip.DepartureStationId = updateDto.DepartureStationId.Value;

					if (updateDto.DepartureTime.HasValue)
						foundTrip.DepartureTime = updateDto.DepartureTime.Value;

					if (updateDto.ArrivalStationId.HasValue)
						foundTrip.ArrivalStationId = updateDto.ArrivalStationId.Value;

					if (updateDto.ArrivalTime.HasValue)
						foundTrip.ArrivalTime = updateDto.ArrivalTime.Value;

					if (updateDto.AvailableSeats.HasValue)
						foundTrip.AvailableSeats = updateDto.AvailableSeats.Value;

					return foundTrip;
				});

			mock.Setup(service => service.DeleteTripAsync(It.IsAny<TripKeyDto>(), It.IsAny<CancellationToken>()))
				.Returns((TripKeyDto tripDto, CancellationToken _) =>
				{
					if (!Trips.Remove(tripDto.TripId))
						throw new ServiceNotFoundException($"Trip with ID {tripDto.TripId} not found.");

					return Task.CompletedTask;
				});

			//Para Seach directamente devolvemos todos los viajes pues no me parece que valga la pena implementar compatibilidad
			// con paginacion y filtros en este mock.
			mock.Setup(service => service.SearchTripsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new PagedResult<Trip>
				{
					Items = Trips.Values.ToList()
				});

			return mock.Object;
		}
	}
}
