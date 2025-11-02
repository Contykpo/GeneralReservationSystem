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
    public class StationServiceTests
    {
        private readonly Mock<IStationRepository> _mockStationRepository;
        private readonly StationService _stationService;

        public StationServiceTests()
        {
            _mockStationRepository = new Mock<IStationRepository>();
            _stationService = new StationService(_mockStationRepository.Object);
        }

        #region CreateStationAsync Tests

        [Fact]
        public async Task CreateStationAsync_ValidDto_ReturnsCreatedStation()
        {
            // Arrange
            CreateStationDto createDto = new()
            {
                StationName = "Central Station",
                City = "Buenos Aires",
                Province = "Buenos Aires",
                Country = "Argentina"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            Station result = await _stationService.CreateStationAsync(createDto);

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
        public async Task CreateStationAsync_DuplicateStation_ThrowsServiceBusinessException()
        {
            // Arrange
            CreateStationDto createDto = new()
            {
                StationName = "Central Station",
                City = "Buenos Aires",
                Province = "Buenos Aires",
                Country = "Argentina"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UniqueConstraintViolationException("Duplicate station"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _stationService.CreateStationAsync(createDto));

            Assert.Equal("Ya existe una estación con el mismo nombre o código.", exception.Message);
            _ = Assert.IsType<UniqueConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateStationAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            CreateStationDto createDto = new()
            {
                StationName = "Central Station",
                City = "Buenos Aires",
                Province = "Buenos Aires",
                Country = "Argentina"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _stationService.CreateStationAsync(createDto));

            Assert.Equal("Error al crear la estación.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateStationAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            CreateStationDto createDto = new()
            {
                StationName = "Central Station",
                City = "Buenos Aires",
                Province = "Buenos Aires",
                Country = "Argentina"
            };
            CancellationToken cancellationToken = new();

            _ = _mockStationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Station>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            _ = await _stationService.CreateStationAsync(createDto, cancellationToken);

            // Assert
            _mockStationRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Station>(), cancellationToken),
                Times.Once);
        }

        #endregion

        #region UpdateStationAsync Tests

        [Fact]
        public async Task UpdateStationAsync_ValidDto_ReturnsUpdatedStation()
        {
            // Arrange
            UpdateStationDto updateDto = new()
            {
                StationId = 1,
                StationName = "Updated Station",
                City = "Córdoba",
                Province = "Córdoba",
                Country = "Argentina"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Station>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            Station result = await _stationService.UpdateStationAsync(updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.StationId, result.StationId);
            Assert.Equal(updateDto.StationName, result.StationName);
            Assert.Equal(updateDto.City, result.City);
            Assert.Equal(updateDto.Province, result.Province);
            Assert.Equal(updateDto.Country, result.Country);

            _mockStationRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Station>(), null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateStationAsync_PartialUpdate_OnlyUpdatesProvidedFields()
        {
            // Arrange
            UpdateStationDto updateDto = new()
            {
                StationId = 1,
                StationName = "Updated Station",
                City = null,
                Province = null,
                Country = null
            };

            _ = _mockStationRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Station>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            Station result = await _stationService.UpdateStationAsync(updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.StationId, result.StationId);
            Assert.Equal(updateDto.StationName, result.StationName);

            _mockStationRepository.Verify(
                repo => repo.UpdateAsync(
                    It.Is<Station>(s => s.StationId == 1 && s.StationName == "Updated Station"),
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateStationAsync_StationNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            UpdateStationDto updateDto = new()
            {
                StationId = 999,
                StationName = "Non-existent Station"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Station>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _stationService.UpdateStationAsync(updateDto));

            Assert.Equal("No se encontró la estación para actualizar.", exception.Message);
        }

        [Fact]
        public async Task UpdateStationAsync_DuplicateName_ThrowsServiceBusinessException()
        {
            // Arrange
            UpdateStationDto updateDto = new()
            {
                StationId = 1,
                StationName = "Duplicate Station"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Station>(), null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UniqueConstraintViolationException("Duplicate station"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _stationService.UpdateStationAsync(updateDto));

            Assert.Equal("Ya existe una estación con el mismo nombre o código.", exception.Message);
            _ = Assert.IsType<UniqueConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task UpdateStationAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            UpdateStationDto updateDto = new()
            {
                StationId = 1,
                StationName = "Updated Station"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Station>(), null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _stationService.UpdateStationAsync(updateDto));

            Assert.Equal("Error al actualizar la estación.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        #endregion

        #region DeleteStationAsync Tests

        [Fact]
        public async Task DeleteStationAsync_ValidKey_DeletesStation()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 1 };

            _ = _mockStationRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _stationService.DeleteStationAsync(keyDto);

            // Assert
            _mockStationRepository.Verify(
                repo => repo.DeleteAsync(
                    It.Is<Station>(s => s.StationId == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteStationAsync_StationNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 999 };

            _ = _mockStationRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _stationService.DeleteStationAsync(keyDto));

            Assert.Equal("No se encontró la estación para eliminar.", exception.Message);
        }

        [Fact]
        public async Task DeleteStationAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 1 };

            _ = _mockStationRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Station>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _stationService.DeleteStationAsync(keyDto));

            Assert.Equal("Error al eliminar la estación.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task DeleteStationAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 1 };
            CancellationToken cancellationToken = new();

            _ = _mockStationRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Station>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            await _stationService.DeleteStationAsync(keyDto, cancellationToken);

            // Assert
            _mockStationRepository.Verify(
                repo => repo.DeleteAsync(It.IsAny<Station>(), cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetStationAsync Tests

        [Fact]
        public async Task GetStationAsync_ValidKey_ReturnsStation()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 1 };
            Station expectedStation = new()
            {
                StationId = 1,
                StationName = "Central Station",
                City = "Buenos Aires",
                Province = "Buenos Aires",
                Country = "Argentina"
            };

            _ = _mockStationRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStation);

            // Act
            Station result = await _stationService.GetStationAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedStation.StationId, result.StationId);
            Assert.Equal(expectedStation.StationName, result.StationName);
            Assert.Equal(expectedStation.City, result.City);
            Assert.Equal(expectedStation.Province, result.Province);
            Assert.Equal(expectedStation.Country, result.Country);

            _mockStationRepository.Verify(
                repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetStationAsync_StationNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 999 };

            _ = _mockStationRepository
                .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Station?)null);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _stationService.GetStationAsync(keyDto));

            Assert.Equal("No se encontró la estación solicitada.", exception.Message);
        }

        [Fact]
        public async Task GetStationAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 1 };

            _ = _mockStationRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _stationService.GetStationAsync(keyDto));

            Assert.Equal("Error al consultar la estación.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetStationAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            StationKeyDto keyDto = new() { StationId = 1 };
            CancellationToken cancellationToken = new();
            Station expectedStation = new() { StationId = 1 };

            _ = _mockStationRepository
                .Setup(repo => repo.GetByIdAsync(1, cancellationToken))
                .ReturnsAsync(expectedStation);

            // Act
            _ = await _stationService.GetStationAsync(keyDto, cancellationToken);

            // Assert
            _mockStationRepository.Verify(
                repo => repo.GetByIdAsync(1, cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetAllStationsAsync Tests

        [Fact]
        public async Task GetAllStationsAsync_ReturnsAllStations()
        {
            // Arrange
            List<Station> expectedStations =
            [
                new Station
                {
                    StationId = 1,
                    StationName = "Station 1",
                    City = "City 1",
                    Province = "Province 1",
                    Country = "Country 1"
                },
                new Station
                {
                    StationId = 2,
                    StationName = "Station 2",
                    City = "City 2",
                    Province = "Province 2",
                    Country = "Country 2"
                },
                new Station
                {
                    StationId = 3,
                    StationName = "Station 3",
                    City = "City 3",
                    Province = "Province 3",
                    Country = "Country 3"
                }
            ];

            _ = _mockStationRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStations);

            // Act
            IEnumerable<Station> result = await _stationService.GetAllStationsAsync();

            // Assert
            Assert.NotNull(result);
            List<Station> stationList = [.. result];
            Assert.Equal(3, stationList.Count);
            Assert.Equal(expectedStations[0].StationId, stationList[0].StationId);
            Assert.Equal(expectedStations[1].StationId, stationList[1].StationId);
            Assert.Equal(expectedStations[2].StationId, stationList[2].StationId);

            _mockStationRepository.Verify(
                repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllStationsAsync_NoStations_ReturnsEmptyCollection()
        {
            // Arrange
            _ = _mockStationRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IEnumerable<Station> result = await _stationService.GetAllStationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockStationRepository.Verify(
                repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllStationsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            _ = _mockStationRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _stationService.GetAllStationsAsync());

            Assert.Equal("Error al obtener la lista de estaciones.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetAllStationsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            CancellationToken cancellationToken = new();
            _ = _mockStationRepository
                .Setup(repo => repo.GetAllAsync(cancellationToken))
                .ReturnsAsync([]);

            // Act
            _ = await _stationService.GetAllStationsAsync(cancellationToken);

            // Assert
            _mockStationRepository.Verify(
                repo => repo.GetAllAsync(cancellationToken),
                Times.Once);
        }

        #endregion

        #region SearchStationsAsync Tests

        [Fact]
        public async Task SearchStationsAsync_ValidRequest_ReturnsPagedResult()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                Filters = [],
                Orders = []
            };

            PagedResult<Station> expectedResult = new()
            {
                Items =
                [
                    new() {
                        StationId = 1,
                        StationName = "Station 1",
                        City = "City 1",
                        Province = "Province 1",
                        Country = "Country 1"
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockStationRepository
                .Setup(repo => repo.SearchAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<Station> result = await _stationService.SearchStationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.TotalCount, result.TotalCount);
            Assert.Equal(expectedResult.Page, result.Page);
            Assert.Equal(expectedResult.PageSize, result.PageSize);
            _ = Assert.Single(result.Items);

            _mockStationRepository.Verify(
                repo => repo.SearchAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchStationsAsync_NoResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                Filters = [],
                Orders = []
            };

            PagedResult<Station> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockStationRepository
                .Setup(repo => repo.SearchAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<Station> result = await _stationService.SearchStationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            _mockStationRepository.Verify(
                repo => repo.SearchAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchStationsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                Filters = [],
                Orders = []
            };

            _ = _mockStationRepository
                .Setup(repo => repo.SearchAsync(searchDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _stationService.SearchStationsAsync(searchDto));

            Assert.Equal("Error al buscar estaciones.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task SearchStationsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                Filters = [],
                Orders = []
            };
            CancellationToken cancellationToken = new();
            PagedResult<Station> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockStationRepository
                .Setup(repo => repo.SearchAsync(searchDto, cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            _ = await _stationService.SearchStationsAsync(searchDto, cancellationToken);

            // Assert
            _mockStationRepository.Verify(
                repo => repo.SearchAsync(searchDto, cancellationToken),
                Times.Once);
        }

        #endregion
    }
}
