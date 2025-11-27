using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Server.Services.Implementations;
using Moq;

namespace GeneralReservationSystem.Tests.Services.API
{
    public class ApiStationServiceTests
    {
        private readonly Mock<IStationRepository> _mockStationRepository;
        private readonly ApiStationService _apiStationService;

        public ApiStationServiceTests()
        {
            _mockStationRepository = new Mock<IStationRepository>();
            _apiStationService = new ApiStationService(_mockStationRepository.Object);
        }

        #region CreateStationsBulkAsync Tests

        [Fact]
        public async Task CreateStationsBulkAsync_ValidStations_ReturnsAffectedCount()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto
                {
                    StationName = "Station 1",
                    City = "City 1",
                    Province = "Province 1",
                    Country = "Country 1"
                },
                new ImportStationDto
                {
                    StationName = "Station 2",
                    City = "City 2",
                    Province = "Province 2",
                    Country = "Country 2"
                },
                new ImportStationDto
                {
                    StationName = "Station 3",
                    City = "City 3",
                    Province = "Province 3",
                    Country = "Country 3"
                }
            ];

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            // Act
            int result = await _apiStationService.CreateStationsBulkAsync(importDtos);

            // Assert
            Assert.Equal(3, result);

            _mockStationRepository.Verify(
                repo => repo.CreateBulkAsync(
                    It.Is<IEnumerable<Station>>(stations =>
                        stations.Count() == 3 &&
                        stations.ElementAt(0).StationName == "Station 1" &&
                        stations.ElementAt(1).StationName == "Station 2" &&
                        stations.ElementAt(2).StationName == "Station 3"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_SingleStation_ReturnsOne()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto
                {
                    StationName = "Single Station",
                    City = "Single City",
                    Province = "Single Province",
                    Country = "Single Country"
                }
            ];

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            int result = await _apiStationService.CreateStationsBulkAsync(importDtos);

            // Assert
            Assert.Equal(1, result);

            _mockStationRepository.Verify(
                repo => repo.CreateBulkAsync(
                    It.Is<IEnumerable<Station>>(stations => stations.Count() == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            List<ImportStationDto> importDtos = [];

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            int result = await _apiStationService.CreateStationsBulkAsync(importDtos);

            // Assert
            Assert.Equal(0, result);

            _mockStationRepository.Verify(
                repo => repo.CreateBulkAsync(
                    It.Is<IEnumerable<Station>>(stations => !stations.Any()),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_LargeNumberOfStations_ReturnsCorrectCount()
        {
            // Arrange
            List<ImportStationDto> importDtos = [.. Enumerable.Range(1, 100).Select(i => new ImportStationDto
            {
                StationName = $"Station {i}",
                City = $"City {i}",
                Province = $"Province {i}",
                Country = $"Country {i}"
            })];

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(100);

            // Act
            int result = await _apiStationService.CreateStationsBulkAsync(importDtos);

            // Assert
            Assert.Equal(100, result);

            _mockStationRepository.Verify(
                repo => repo.CreateBulkAsync(
                    It.Is<IEnumerable<Station>>(stations => stations.Count() == 100),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_DuplicateStationNames_ThrowsServiceDuplicateException()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto
                {
                    StationName = "Station 1",
                    City = "City 1",
                    Province = "Province 1",
                    Country = "Country 1"
                },
                new ImportStationDto
                {
                    StationName = "Station 1", // Duplicate
                    City = "City 2",
                    Province = "Province 2",
                    Country = "Country 2"
                }
            ];

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UniqueConstraintViolationException("UQ_Station_Name"));

            // Act & Assert
            ServiceDuplicateException exception = await Assert.ThrowsAsync<ServiceDuplicateException>(
                () => _apiStationService.CreateStationsBulkAsync(importDtos));

            Assert.Equal("Una o más estaciones tienen nombres duplicados/ya registrados.", exception.Message);
            _ = Assert.IsType<UniqueConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_StationAlreadyExists_ThrowsServiceDuplicateException()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto
                {
                    StationName = "Existing Station",
                    City = "City 1",
                    Province = "Province 1",
                    Country = "Country 1"
                }
            ];

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UniqueConstraintViolationException("UQ_Station_Name"));

            // Act & Assert
            ServiceDuplicateException exception = await Assert.ThrowsAsync<ServiceDuplicateException>(
                () => _apiStationService.CreateStationsBulkAsync(importDtos));

            Assert.Equal("Una o más estaciones tienen nombres duplicados/ya registrados.", exception.Message);
            _ = Assert.IsType<UniqueConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto
                {
                    StationName = "Station 1",
                    City = "City 1",
                    Province = "Province 1",
                    Country = "Country 1"
                }
            ];

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _apiStationService.CreateStationsBulkAsync(importDtos));

            Assert.Equal("Error al crear las estaciones en lote.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_MapsPropertiesCorrectly_CreatesStationsWithCorrectData()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto
                {
                    StationName = "Test Station",
                    City = "Test City",
                    Province = "Test Province",
                    Country = "Test Country"
                }
            ];

            List<Station>? capturedStations = null;
            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .Callback<IEnumerable<Station>, CancellationToken>((stations, ct) =>
                {
                    capturedStations = [.. stations];
                });

            // Act
            _ = await _apiStationService.CreateStationsBulkAsync(importDtos);

            // Assert
            Assert.NotNull(capturedStations);
            _ = Assert.Single(capturedStations);
            Station station = capturedStations[0];
            Assert.Equal("Test Station", station.StationName);
            Assert.Equal("Test City", station.City);
            Assert.Equal("Test Province", station.Province);
            Assert.Equal("Test Country", station.Country);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto
                {
                    StationName = "Station 1",
                    City = "City 1",
                    Province = "Province 1",
                    Country = "Country 1"
                }
            ];
            CancellationToken cancellationToken = new();

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            _ = await _apiStationService.CreateStationsBulkAsync(importDtos, cancellationToken);

            // Assert
            _mockStationRepository.Verify(
                repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), cancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_PartialSuccess_ReturnsActualAffectedCount()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto { StationName = "Station 1", City = "City 1", Province = "Province 1", Country = "Country 1" },
                new ImportStationDto { StationName = "Station 2", City = "City 2", Province = "Province 2", Country = "Country 2" },
                new ImportStationDto { StationName = "Station 3", City = "City 3", Province = "Province 3", Country = "Country 3" }
            ];

            // Simulating partial success - only 2 out of 3 stations were created
            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            // Act
            int result = await _apiStationService.CreateStationsBulkAsync(importDtos);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task CreateStationsBulkAsync_VariedStationData_CreatesAllStations()
        {
            // Arrange
            List<ImportStationDto> importDtos =
            [
                new ImportStationDto
                {
                    StationName = "Buenos Aires Central",
                    City = "Buenos Aires",
                    Province = "Buenos Aires",
                    Country = "Argentina"
                },
                new ImportStationDto
                {
                    StationName = "Córdoba Terminal",
                    City = "Córdoba",
                    Province = "Córdoba",
                    Country = "Argentina"
                },
                new ImportStationDto
                {
                    StationName = "Rosario Norte",
                    City = "Rosario",
                    Province = "Santa Fe",
                    Country = "Argentina"
                }
            ];

            _ = _mockStationRepository
                .Setup(repo => repo.CreateBulkAsync(It.IsAny<IEnumerable<Station>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            // Act
            int result = await _apiStationService.CreateStationsBulkAsync(importDtos);

            // Assert
            Assert.Equal(3, result);

            _mockStationRepository.Verify(
                repo => repo.CreateBulkAsync(
                    It.Is<IEnumerable<Station>>(stations =>
                        stations.Any(s => s.StationName == "Buenos Aires Central") &&
                        stations.Any(s => s.StationName == "Córdoba Terminal") &&
                        stations.Any(s => s.StationName == "Rosario Norte")),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Inherited Methods Tests (Verification that base class methods still work)

        [Fact]
        public async Task CreateStationAsync_InheritedFromBase_WorksCorrectly()
        {
            // Arrange
            CreateStationDto createDto = new()
            {
                StationName = "New Station",
                City = "New City",
                Province = "New Province",
                Country = "New Country"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            Station result = await _apiStationService.CreateStationAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.StationName, result.StationName);
            Assert.Equal(createDto.City, result.City);
            Assert.Equal(createDto.Province, result.Province);
            Assert.Equal(createDto.Country, result.Country);

            _mockStationRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetStationAsync_InheritedFromBase_WorksCorrectly()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 1 };
            Station expectedStation = new()
            {
                StationId = 1,
                StationName = "Test Station",
                City = "Test City",
                Province = "Test Province",
                Country = "Test Country"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStation);

            // Act
            Station result = await _apiStationService.GetStationAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedStation.StationId, result.StationId);
            Assert.Equal(expectedStation.StationName, result.StationName);

            _mockStationRepository.Verify(
                repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllStationsAsync_InheritedFromBase_WorksCorrectly()
        {
            // Arrange
            List<Station> expectedStations =
            [
                new Station { StationId = 1, StationName = "Station 1", City = "City 1", Province = "Province 1", Country = "Country 1" },
                new Station { StationId = 2, StationName = "Station 2", City = "City 2", Province = "Province 2", Country = "Country 2" }
            ];

            _ = _mockStationRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStations);

            // Act
            IEnumerable<Station> result = await _apiStationService.GetAllStationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());

            _mockStationRepository.Verify(
                repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion
    }
}
