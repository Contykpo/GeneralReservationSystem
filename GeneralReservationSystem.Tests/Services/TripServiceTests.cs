using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.DefaultImplementations;
using Moq;

namespace GeneralReservationSystem.Tests.Services
{
    public class TripServiceTests
    {
        private readonly Mock<ITripRepository> _mockTripRepository;
        private readonly TripService _tripService;

        public TripServiceTests()
        {
            _mockTripRepository = new Mock<ITripRepository>();
            _tripService = new TripService(_mockTripRepository.Object);
        }

        #region CreateTripAsync Tests

        [Fact]
        public async Task CreateTripAsync_ValidDto_ReturnsCreatedTrip()
        {
            // Arrange
            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 2,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            Trip result = await _tripService.CreateTripAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.DepartureStationId, result.DepartureStationId);
            Assert.Equal(createDto.DepartureTime, result.DepartureTime);
            Assert.Equal(createDto.ArrivalStationId, result.ArrivalStationId);
            Assert.Equal(createDto.ArrivalTime, result.ArrivalTime);
            Assert.Equal(createDto.AvailableSeats, result.AvailableSeats);

            _mockTripRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateTripAsync_InvalidStations_ThrowsServiceBusinessException()
        {
            // Arrange
            CreateTripDto createDto = new()
            {
                DepartureStationId = 999,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 998,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ForeignKeyViolationException("FK_Trip_Station"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _tripService.CreateTripAsync(createDto));

            Assert.Equal("La estación de salida o llegada no existe.", exception.Message);
            _ = Assert.IsType<ForeignKeyViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateTripAsync_SameDepartureAndArrivalStation_ThrowsServiceBusinessException()
        {
            // Arrange
            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 1,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CheckConstraintViolationException("CK_Trip_Departure_Arrival"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _tripService.CreateTripAsync(createDto));

            Assert.Equal("La estación de salida y llegada deben ser diferentes.", exception.Message);
            _ = Assert.IsType<CheckConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateTripAsync_ArrivalTimeBeforeDepartureTime_ThrowsServiceBusinessException()
        {
            // Arrange
            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 14, 0, 0),
                ArrivalStationId = 2,
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CheckConstraintViolationException("CK_Trip_Times"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _tripService.CreateTripAsync(createDto));

            Assert.Equal("La hora de llegada debe ser posterior a la de salida.", exception.Message);
            _ = Assert.IsType<CheckConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateTripAsync_InvalidAvailableSeats_ThrowsServiceBusinessException()
        {
            // Arrange
            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 2,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = -5
            };

            _ = _mockTripRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CheckConstraintViolationException("CK_Trip_AvailableSeats"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _tripService.CreateTripAsync(createDto));

            Assert.Equal("El número de asientos disponibles debe ser un número positivo.", exception.Message);
            _ = Assert.IsType<CheckConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateTripAsync_UnknownCheckConstraint_ThrowsServiceBusinessException()
        {
            // Arrange
            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 2,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CheckConstraintViolationException("CK_Trip_Unknown"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _tripService.CreateTripAsync(createDto));

            Assert.Equal("Restricción de datos inválida en el viaje.", exception.Message);
            _ = Assert.IsType<CheckConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateTripAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 2,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _tripService.CreateTripAsync(createDto));

            Assert.Equal("Error al crear el viaje.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateTripAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 2,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };
            CancellationToken cancellationToken = new();

            _ = _mockTripRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Trip>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            _ = await _tripService.CreateTripAsync(createDto, cancellationToken);

            // Assert
            _mockTripRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Trip>(), cancellationToken),
                Times.Once);
        }

        #endregion

        #region DeleteTripAsync Tests

        [Fact]
        public async Task DeleteTripAsync_ValidKey_DeletesTrip()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };

            _ = _mockTripRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _tripService.DeleteTripAsync(keyDto);

            // Assert
            _mockTripRepository.Verify(
                repo => repo.DeleteAsync(
                    It.Is<Trip>(t => t.TripId == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteTripAsync_TripNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 999 };

            _ = _mockTripRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _tripService.DeleteTripAsync(keyDto));

            Assert.Equal("No se encontró el viaje para eliminar.", exception.Message);
        }

        [Fact]
        public async Task DeleteTripAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };

            _ = _mockTripRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _tripService.DeleteTripAsync(keyDto));

            Assert.Equal("Error al eliminar el viaje.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task DeleteTripAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };
            CancellationToken cancellationToken = new();

            _ = _mockTripRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Trip>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            await _tripService.DeleteTripAsync(keyDto, cancellationToken);

            // Assert
            _mockTripRepository.Verify(
                repo => repo.DeleteAsync(It.IsAny<Trip>(), cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetTripAsync Tests

        [Fact]
        public async Task GetTripAsync_ValidKey_ReturnsTrip()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };
            Trip expectedTrip = new()
            {
                TripId = 1,
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 2,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTrip);

            // Act
            Trip result = await _tripService.GetTripAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTrip.TripId, result.TripId);
            Assert.Equal(expectedTrip.DepartureStationId, result.DepartureStationId);
            Assert.Equal(expectedTrip.DepartureTime, result.DepartureTime);
            Assert.Equal(expectedTrip.ArrivalStationId, result.ArrivalStationId);
            Assert.Equal(expectedTrip.ArrivalTime, result.ArrivalTime);
            Assert.Equal(expectedTrip.AvailableSeats, result.AvailableSeats);

            _mockTripRepository.Verify(
                repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTripAsync_TripNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 999 };

            _ = _mockTripRepository
                .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Trip?)null);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _tripService.GetTripAsync(keyDto));

            Assert.Equal("No se encontró el viaje solicitado.", exception.Message);
        }

        [Fact]
        public async Task GetTripAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };

            _ = _mockTripRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _tripService.GetTripAsync(keyDto));

            Assert.Equal("Error al consultar el viaje.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetTripAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };
            CancellationToken cancellationToken = new();
            Trip expectedTrip = new() { TripId = 1 };

            _ = _mockTripRepository
                .Setup(repo => repo.GetByIdAsync(1, cancellationToken))
                .ReturnsAsync(expectedTrip);

            // Act
            _ = await _tripService.GetTripAsync(keyDto, cancellationToken);

            // Assert
            _mockTripRepository.Verify(
                repo => repo.GetByIdAsync(1, cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetTripWithDetailsAsync Tests

        [Fact]
        public async Task GetTripWithDetailsAsync_ValidKey_ReturnsTripWithDetails()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };
            TripWithDetailsDto expectedTripDetails = new()
            {
                TripId = 1,
                DepartureStationId = 1,
                DepartureStationName = "Station A",
                DepartureCity = "City A",
                DepartureProvince = "Province A",
                DepartureCountry = "Country A",
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 2,
                ArrivalStationName = "Station B",
                ArrivalCity = "City B",
                ArrivalProvince = "Province B",
                ArrivalCountry = "Country B",
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50,
                ReservedSeats = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.GetTripWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTripDetails);

            // Act
            TripWithDetailsDto result = await _tripService.GetTripWithDetailsAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTripDetails.TripId, result.TripId);
            Assert.Equal(expectedTripDetails.DepartureStationName, result.DepartureStationName);
            Assert.Equal(expectedTripDetails.ArrivalStationName, result.ArrivalStationName);
            Assert.Equal(expectedTripDetails.ReservedSeats, result.ReservedSeats);

            _mockTripRepository.Verify(
                repo => repo.GetTripWithDetailsAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTripWithDetailsAsync_TripNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 999 };

            _ = _mockTripRepository
                .Setup(repo => repo.GetTripWithDetailsAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripWithDetailsDto?)null);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _tripService.GetTripWithDetailsAsync(keyDto));

            Assert.Equal("No se encontró el viaje solicitado.", exception.Message);
        }

        [Fact]
        public async Task GetTripWithDetailsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };

            _ = _mockTripRepository
                .Setup(repo => repo.GetTripWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _tripService.GetTripWithDetailsAsync(keyDto));

            Assert.Equal("Error al buscar viajes.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetTripWithDetailsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };
            CancellationToken cancellationToken = new();
            TripWithDetailsDto expectedTripDetails = new() { TripId = 1 };

            _ = _mockTripRepository
                .Setup(repo => repo.GetTripWithDetailsAsync(1, cancellationToken))
                .ReturnsAsync(expectedTripDetails);

            // Act
            _ = await _tripService.GetTripWithDetailsAsync(keyDto, cancellationToken);

            // Assert
            _mockTripRepository.Verify(
                repo => repo.GetTripWithDetailsAsync(1, cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetAllTripsAsync Tests

        [Fact]
        public async Task GetAllTripsAsync_ReturnsAllTrips()
        {
            // Arrange
            List<Trip> expectedTrips =
            [
                new Trip
                {
                    TripId = 1,
                    DepartureStationId = 1,
                    DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                    ArrivalStationId = 2,
                    ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                    AvailableSeats = 50
                },
                new Trip
                {
                    TripId = 2,
                    DepartureStationId = 2,
                    DepartureTime = new DateTime(2024, 6, 2, 11, 0, 0),
                    ArrivalStationId = 3,
                    ArrivalTime = new DateTime(2024, 6, 2, 15, 0, 0),
                    AvailableSeats = 40
                }
            ];

            _ = _mockTripRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTrips);

            // Act
            IEnumerable<Trip> result = await _tripService.GetAllTripsAsync();

            // Assert
            Assert.NotNull(result);
            List<Trip> tripList = [.. result];
            Assert.Equal(2, tripList.Count);
            Assert.Equal(expectedTrips[0].TripId, tripList[0].TripId);
            Assert.Equal(expectedTrips[1].TripId, tripList[1].TripId);

            _mockTripRepository.Verify(
                repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllTripsAsync_NoTrips_ReturnsEmptyCollection()
        {
            // Arrange
            _ = _mockTripRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IEnumerable<Trip> result = await _tripService.GetAllTripsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockTripRepository.Verify(
                repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllTripsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            _ = _mockTripRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _tripService.GetAllTripsAsync());

            Assert.Equal("Error al obtener la lista de viajes.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetAllTripsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            CancellationToken cancellationToken = new();
            _ = _mockTripRepository
                .Setup(repo => repo.GetAllAsync(cancellationToken))
                .ReturnsAsync([]);

            // Act
            _ = await _tripService.GetAllTripsAsync(cancellationToken);

            // Assert
            _mockTripRepository.Verify(
                repo => repo.GetAllAsync(cancellationToken),
                Times.Once);
        }

        #endregion

        #region SearchTripsAsync Tests

        [Fact]
        public async Task SearchTripsAsync_ValidRequest_ReturnsPagedResult()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() {
                        TripId = 1,
                        DepartureStationId = 1,
                        DepartureStationName = "Station A",
                        ArrivalStationId = 2,
                        ArrivalStationName = "Station B",
                        DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                        ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                        AvailableSeats = 50,
                        ReservedSeats = 10
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.TotalCount, result.TotalCount);
            Assert.Equal(expectedResult.Page, result.Page);
            Assert.Equal(expectedResult.PageSize, result.PageSize);
            _ = Assert.Single(result.Items);

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_NoResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFilterEquals_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("TripId", FilterOperator.Equals, 1)
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureStationName = "Station A", ArrivalStationName = "Station B" }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, item => Assert.Equal(1, item.TripId));

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFilterContains_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureCity", FilterOperator.Contains, "Buenos")
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureCity = "Buenos Aires", ArrivalCity = "Córdoba" },
                    new() { TripId = 2, DepartureCity = "Buenos Aires", ArrivalCity = "Rosario" }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.Contains("Buenos", item.DepartureCity));

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFilterStartsWith_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureStationName", FilterOperator.StartsWith, "Central")
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureStationName = "Central Station" },
                    new() { TripId = 2, DepartureStationName = "Central Terminal" }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.StartsWith("Central", item.DepartureStationName));

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFilterGreaterThan_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("AvailableSeats", FilterOperator.GreaterThan, 30)
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, AvailableSeats = 50 },
                    new() { TripId = 2, AvailableSeats = 40 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.AvailableSeats > 30));

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFilterLessThanOrEqual_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("ReservedSeats", FilterOperator.LessThanOrEqual, 10)
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, ReservedSeats = 5 },
                    new() { TripId = 2, ReservedSeats = 10 },
                    new() { TripId = 3, ReservedSeats = 8 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.ReservedSeats <= 10));

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFilterBetween_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureTime", FilterOperator.Between, new object[] { new DateTime(2024, 6, 1), new DateTime(2024, 6, 30) })
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureTime = new DateTime(2024, 6, 5, 10, 0, 0) },
                    new() { TripId = 2, DepartureTime = new DateTime(2024, 6, 15, 14, 0, 0) },
                    new() { TripId = 3, DepartureTime = new DateTime(2024, 6, 25, 8, 0, 0) }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.DepartureTime >= new DateTime(2024, 6, 1));
                Assert.True(item.DepartureTime <= new DateTime(2024, 6, 30));
            });

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFilterNotEquals_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureStationId", FilterOperator.NotEquals, 1)
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 2, DepartureStationId = 2 },
                    new() { TripId = 3, DepartureStationId = 3 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.NotEqual(1, item.DepartureStationId));

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFilterEndsWith_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("ArrivalCountry", FilterOperator.EndsWith, "tina")
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, ArrivalCountry = "Argentina" }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, item => Assert.EndsWith("tina", item.ArrivalCountry));

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithMultipleFiltersInSameClause_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureStationId", FilterOperator.Equals, 1),
                        new Filter("DepartureStationId", FilterOperator.Equals, 2)
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureStationId = 1 },
                    new() { TripId = 2, DepartureStationId = 2 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.DepartureStationId is 1 or 2));

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithMultipleFilterClauses_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("AvailableSeats", FilterOperator.GreaterThan, 20)
                    ]),
                    new FilterClause(
                    [
                        new Filter("ReservedSeats", FilterOperator.LessThan, 15)
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, AvailableSeats = 50, ReservedSeats = 10 },
                    new() { TripId = 2, AvailableSeats = 40, ReservedSeats = 12 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.AvailableSeats > 20);
                Assert.True(item.ReservedSeats < 15);
            });

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithOrderByAscending_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("DepartureTime", SortDirection.Asc)
                ]
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureTime = new DateTime(2024, 6, 1, 8, 0, 0) },
                    new() { TripId = 2, DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0) },
                    new() { TripId = 3, DepartureTime = new DateTime(2024, 6, 1, 14, 0, 0) }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<TripWithDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].DepartureTime >= items[i - 1].DepartureTime);
            }

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithOrderByDescending_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("AvailableSeats", SortDirection.Desc)
                ]
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, AvailableSeats = 60 },
                    new() { TripId = 2, AvailableSeats = 50 },
                    new() { TripId = 3, AvailableSeats = 40 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<TripWithDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].AvailableSeats <= items[i - 1].AvailableSeats);
            }

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithMultipleOrders_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("DepartureStationId", SortDirection.Asc),
                    new SortOption("DepartureTime", SortDirection.Asc)
                ]
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureStationId = 1, DepartureTime = new DateTime(2024, 6, 1, 8, 0, 0) },
                    new() { TripId = 2, DepartureStationId = 1, DepartureTime = new DateTime(2024, 6, 1, 14, 0, 0) },
                    new() { TripId = 3, DepartureStationId = 2, DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0) }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<TripWithDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].DepartureStationId >= items[i - 1].DepartureStationId);
                if (items[i].DepartureStationId == items[i - 1].DepartureStationId)
                {
                    Assert.True(items[i].DepartureTime >= items[i - 1].DepartureTime);
                }
            }

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithFiltersAndOrders_ReturnsFilteredAndOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("AvailableSeats", FilterOperator.GreaterThanOrEqual, 30)
                    ])
                ],
                Orders =
                [
                    new SortOption("DepartureTime", SortDirection.Asc)
                ]
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, AvailableSeats = 50, DepartureTime = new DateTime(2024, 6, 1, 8, 0, 0) },
                    new() { TripId = 2, AvailableSeats = 40, DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0) },
                    new() { TripId = 3, AvailableSeats = 30, DepartureTime = new DateTime(2024, 6, 1, 14, 0, 0) }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.AvailableSeats >= 30));
            List<TripWithDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].DepartureTime >= items[i - 1].DepartureTime);
            }

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithComplexFilteringAndOrdering_ReturnsCorrectResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureCity", FilterOperator.Contains, "Buenos"),
                        new Filter("DepartureCity", FilterOperator.Contains, "Córdoba")
                    ]),
                    new FilterClause(
                    [
                        new Filter("AvailableSeats", FilterOperator.GreaterThan, 25)
                    ])
                ],
                Orders =
                [
                    new SortOption("DepartureCity", SortDirection.Asc),
                    new SortOption("AvailableSeats", SortDirection.Desc)
                ]
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureCity = "Buenos Aires", AvailableSeats = 50 },
                    new() { TripId = 2, DepartureCity = "Buenos Aires", AvailableSeats = 40 },
                    new() { TripId = 3, DepartureCity = "Córdoba", AvailableSeats = 45 },
                    new() { TripId = 4, DepartureCity = "Córdoba", AvailableSeats = 30 }
                ],
                TotalCount = 4,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.DepartureCity.Contains("Buenos") || item.DepartureCity.Contains("Córdoba"));
                Assert.True(item.AvailableSeats > 25);
            });
            List<TripWithDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                int cityComparison = string.Compare(items[i].DepartureCity, items[i - 1].DepartureCity, StringComparison.Ordinal);
                Assert.True(cityComparison >= 0);
                if (cityComparison == 0)
                {
                    Assert.True(items[i].AvailableSeats <= items[i - 1].AvailableSeats);
                }
            }

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_WithDateTimeFiltering_ReturnsFilteredResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureTime", FilterOperator.GreaterThanOrEqual, new DateTime(2024, 6, 1)),
                        new Filter("DepartureTime", FilterOperator.LessThan, new DateTime(2024, 7, 1))
                    ])
                ],
                Orders = []
            };

            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureTime = new DateTime(2024, 6, 5, 10, 0, 0) },
                    new() { TripId = 2, DepartureTime = new DateTime(2024, 6, 15, 14, 0, 0) }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<TripWithDetailsDto> result = await _tripService.SearchTripsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.DepartureTime >= new DateTime(2024, 6, 1) || item.DepartureTime < new DateTime(2024, 7, 1));
            });

            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchTripsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _tripService.SearchTripsAsync(searchDto));

            Assert.Equal("Error al buscar viajes.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task SearchTripsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };
            CancellationToken cancellationToken = new();
            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            _ = await _tripService.SearchTripsAsync(searchDto, cancellationToken);

            // Assert
            _mockTripRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetFreeSeatsAsync Tests

        [Fact]
        public async Task GetFreeSeatsAsync_ValidKey_ReturnsFreeSeats()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };
            List<int> expectedFreeSeats = [1, 2, 3, 5, 7, 9, 10];

            _ = _mockTripRepository
                .Setup(repo => repo.GetFreeSeatsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedFreeSeats);

            // Act
            IEnumerable<int> result = await _tripService.GetFreeSeatsAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            List<int> seatList = [.. result];
            Assert.Equal(expectedFreeSeats.Count, seatList.Count);
            Assert.Equal(expectedFreeSeats, seatList);

            _mockTripRepository.Verify(
                repo => repo.GetFreeSeatsAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetFreeSeatsAsync_NoFreeSeats_ReturnsEmptyCollection()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };

            _ = _mockTripRepository
                .Setup(repo => repo.GetFreeSeatsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IEnumerable<int> result = await _tripService.GetFreeSeatsAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockTripRepository.Verify(
                repo => repo.GetFreeSeatsAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetFreeSeatsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };

            _ = _mockTripRepository
                .Setup(repo => repo.GetFreeSeatsAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _tripService.GetFreeSeatsAsync(keyDto));

            Assert.Equal("Error al obtener los asientos libres.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetFreeSeatsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            TripKeyDto keyDto = new() { TripId = 1 };
            CancellationToken cancellationToken = new();
            List<int> expectedFreeSeats = [1, 2, 3];

            _ = _mockTripRepository
                .Setup(repo => repo.GetFreeSeatsAsync(1, cancellationToken))
                .ReturnsAsync(expectedFreeSeats);

            // Act
            _ = await _tripService.GetFreeSeatsAsync(keyDto, cancellationToken);

            // Assert
            _mockTripRepository.Verify(
                repo => repo.GetFreeSeatsAsync(1, cancellationToken),
                Times.Once);
        }

        #endregion
    }
}
