using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.Common;

using Moq;

namespace GeneralReservationSystem.MockServices
{
	public static class MockStationService
	{
		public static readonly Dictionary<int, Station> Stations = new()
		{
			{ 1, new Station { StationId = 1, StationName = "Retiro", Country = "Argentina", City = "Buenos Aires" } },
			{ 2, new Station { StationId = 2, StationName = "Constitución", Country = "Argentina", City = "Buenos Aires" } },
			{ 3, new Station { StationId = 3, StationName = "Rosario Norte", Country = "Argentina", City = "Rosario" } },
			{ 4, new Station { StationId = 4, StationName = "Córdoba Mitre", Country = "Argentina", City = "Córdoba" } },
			{ 5, new Station { StationId = 5, StationName = "Mar del Plata", Country = "Argentina", City = "Mar del Plata" } },
			{ 6, new Station { StationId = 6, StationName = "Bahía Blanca Sud", Country = "Argentina", City = "Bahía Blanca" } },
			{ 7, new Station { StationId = 7, StationName = "Tucumán", Country = "Argentina", City = "San Miguel de Tucumán" } },
			{ 8, new Station { StationId = 8, StationName = "Salta", Country = "Argentina", City = "Salta" } },
			{ 9, new Station { StationId = 9, StationName = "Posadas", Country = "Argentina", City = "Posadas" } },
			{ 10, new Station { StationId = 10, StationName = "Mendoza", Country = "Argentina", City = "Mendoza" } }
		};

		public static IStationService GetService()
		{
			var mock = new Moq.Mock<IStationService>();

			mock.Setup(service => service.GetAllStationsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(Stations.Values);

			mock.Setup(service => service.GetStationAsync(It.IsAny<StationKeyDto>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((StationKeyDto stationKey, CancellationToken _) =>
				{
					if (!Stations.TryGetValue(stationKey.StationId, out var foundStation))
						throw new ServiceNotFoundException($"Station with ID {stationKey.StationId} not found.");

					return foundStation;
				});

			mock.Setup(service => service.CreateStationAsync(It.IsAny<CreateStationDto>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((CreateStationDto createDto, CancellationToken _) =>
				{
					var newStationId = Stations.Keys.Max() + 1;

					var newStation = new Station
					{
						StationId	= newStationId,
						StationName = createDto.StationName,
						Country		= createDto.Country,
						City		= createDto.City
					};

					Stations.Add(newStation.StationId, newStation);

					return newStation;
				});

			mock.Setup(service => service.UpdateStationAsync(It.IsAny<UpdateStationDto>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((UpdateStationDto updateDto, CancellationToken _) =>
				{
					if (!Stations.TryGetValue(updateDto.StationId, out Station foundStation))
						throw new ServiceNotFoundException($"Station with ID {updateDto.StationId} not found.");

					foundStation.StationName	= updateDto.StationName;
					foundStation.Country		= updateDto.Country;
					foundStation.City			= updateDto.City;

					return foundStation;
				});

			mock.Setup(service => service.DeleteStationAsync(It.IsAny<StationKeyDto>(), It.IsAny<CancellationToken>()))
				.Returns((StationKeyDto stationKey, CancellationToken _) =>
				{
					if (!Stations.Remove(stationKey.StationId))
						throw new ServiceNotFoundException($"Station with ID {stationKey.StationId} not found.");

					return Task.CompletedTask;
				});

			//Idem a trip, no vamos a soportar busquedas complejas en los mocks, asi que directamente devolvemos todo
			mock.Setup(service => service.SearchStationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new PagedResult<Station>
				{
					Items = Stations.Values.ToList()
				});

			return mock.Object;
		}
	}
}
