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
			{ 1,  new Trip { TripId = 1,  DepartureStationId = 1, ArrivalStationId = 2, AvailableSeats = 25, DepartureTime = new DateTime(2025, 10, 16, 8, 30, 0),		ArrivalTime = new DateTime(2025, 10, 16, 12, 0, 0) } },
			{ 2,  new Trip { TripId = 2,  DepartureStationId = 2, ArrivalStationId = 3, AvailableSeats = 40, DepartureTime = new DateTime(2025, 10, 16, 14, 0, 0),		ArrivalTime = new DateTime(2025, 10, 16, 18, 45, 0) } },
			{ 3,  new Trip { TripId = 3,  DepartureStationId = 3, ArrivalStationId = 4, AvailableSeats = 15, DepartureTime = new DateTime(2025, 10, 17, 7, 15, 0),		ArrivalTime = new DateTime(2025, 10, 17, 13, 30, 0) } },
			{ 4,  new Trip { TripId = 4,  DepartureStationId = 1, ArrivalStationId = 5, AvailableSeats = 50, DepartureTime = new DateTime(2025, 10, 18, 6, 0, 0),		ArrivalTime = new DateTime(2025, 10, 18, 13, 15, 0) } },
			{ 5,  new Trip { TripId = 5,  DepartureStationId = 5, ArrivalStationId = 6, AvailableSeats = 30, DepartureTime = new DateTime(2025, 10, 18, 16, 45, 0),		ArrivalTime = new DateTime(2025, 10, 18, 21, 0, 0) } },
			{ 6,  new Trip { TripId = 6,  DepartureStationId = 6, ArrivalStationId = 7, AvailableSeats = 12, DepartureTime = new DateTime(2025, 10, 19, 9, 0, 0),		ArrivalTime = new DateTime(2025, 10, 19, 19, 30, 0) } },
			{ 7,  new Trip { TripId = 7,  DepartureStationId = 7, ArrivalStationId = 8, AvailableSeats = 18, DepartureTime = new DateTime(2025, 10, 20, 8, 0, 0),		ArrivalTime = new DateTime(2025, 10, 20, 14, 30, 0) } },
			{ 8,  new Trip { TripId = 8,  DepartureStationId = 8, ArrivalStationId = 9, AvailableSeats = 20, DepartureTime = new DateTime(2025, 10, 21, 11, 0, 0),		ArrivalTime = new DateTime(2025, 10, 21, 18, 0, 0) } },
			{ 9,  new Trip { TripId = 9,  DepartureStationId = 9, ArrivalStationId = 10, AvailableSeats = 22, DepartureTime = new DateTime(2025, 10, 22, 10, 30, 0),	ArrivalTime = new DateTime(2025, 10, 22, 20, 0, 0) } },
			{ 10, new Trip { TripId = 10, DepartureStationId = 10, ArrivalStationId = 1, AvailableSeats = 35, DepartureTime = new DateTime(2025, 10, 23, 7, 45, 0),		ArrivalTime = new DateTime(2025, 10, 23, 17, 0, 0) } }
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
