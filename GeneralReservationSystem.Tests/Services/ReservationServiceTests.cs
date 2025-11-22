using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Services.DefaultImplementations;
using Moq;

namespace GeneralReservationSystem.Tests.Services
{
    public class ReservationServiceTests
    {
        private readonly Mock<IReservationRepository> _mockReservationRepository;
        private readonly ReservationService _reservationService;

        public ReservationServiceTests()
        {
            _mockReservationRepository = new Mock<IReservationRepository>();
            _reservationService = new ReservationService(_mockReservationRepository.Object);
        }

        #region CreateReservationAsync Tests

        [Fact]
        public async Task CreateReservationAsync_ValidDto_ReturnsCreatedReservation()
        {
            // Arrange
            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            Reservation result = await _reservationService.CreateReservationAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.TripId, result.TripId);
            Assert.Equal(createDto.UserId, result.UserId);
            Assert.Equal(createDto.Seat, result.Seat);

            _mockReservationRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateReservationAsync_InvalidTripOrUser_ThrowsServiceBusinessException()
        {
            // Arrange
            CreateReservationDto createDto = new()
            {
                TripId = 999,
                UserId = 998,
                Seat = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ForeignKeyViolationException("FK_Reservation_Trip_User"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _reservationService.CreateReservationAsync(createDto));

            Assert.Equal("El viaje o el usuario no existen.", exception.Message);
            _ = Assert.IsType<ForeignKeyViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateReservationAsync_SeatAlreadyReserved_ThrowsServiceBusinessException()
        {
            // Arrange
            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UniqueConstraintViolationException("UQ_Reservation_Trip_Seat"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _reservationService.CreateReservationAsync(createDto));

            Assert.Equal("El asiento ya está reservado para este viaje.", exception.Message);
            _ = Assert.IsType<UniqueConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateReservationAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _reservationService.CreateReservationAsync(createDto));

            Assert.Equal("Error al crear la reserva.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task CreateReservationAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };
            CancellationToken cancellationToken = new();

            _ = _mockReservationRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Reservation>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            _ = await _reservationService.CreateReservationAsync(createDto, cancellationToken);

            // Assert
            _mockReservationRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Reservation>(), cancellationToken),
                Times.Once);
        }

        #endregion

        #region DeleteReservationAsync Tests

        [Fact]
        public async Task DeleteReservationAsync_ValidKey_DeletesReservation()
        {
            // Arrange
            ReservationKeyDto keyDto = new()
            {
                TripId = 1,
                Seat = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _reservationService.DeleteReservationAsync(keyDto);

            // Assert
            _mockReservationRepository.Verify(
                repo => repo.DeleteAsync(
                    It.Is<Reservation>(r => r.TripId == 1 && r.Seat == 5),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteReservationAsync_ReservationNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            ReservationKeyDto keyDto = new()
            {
                TripId = 999,
                Seat = 99
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _reservationService.DeleteReservationAsync(keyDto));

            Assert.Equal("No se encontró la reserva para eliminar.", exception.Message);
        }

        [Fact]
        public async Task DeleteReservationAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            ReservationKeyDto keyDto = new()
            {
                TripId = 1,
                Seat = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _reservationService.DeleteReservationAsync(keyDto));

            Assert.Equal("Error al eliminar la reserva.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task DeleteReservationAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            ReservationKeyDto keyDto = new()
            {
                TripId = 1,
                Seat = 5
            };
            CancellationToken cancellationToken = new();

            _ = _mockReservationRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Reservation>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            await _reservationService.DeleteReservationAsync(keyDto, cancellationToken);

            // Assert
            _mockReservationRepository.Verify(
                repo => repo.DeleteAsync(It.IsAny<Reservation>(), cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetReservationAsync Tests

        [Fact]
        public async Task GetReservationAsync_ValidKey_ReturnsReservation()
        {
            // Arrange
            ReservationKeyDto keyDto = new()
            {
                TripId = 1,
                Seat = 5
            };
            Reservation expectedReservation = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.GetByKeyAsync(1, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReservation);

            // Act
            Reservation result = await _reservationService.GetReservationAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedReservation.TripId, result.TripId);
            Assert.Equal(expectedReservation.UserId, result.UserId);
            Assert.Equal(expectedReservation.Seat, result.Seat);

            _mockReservationRepository.Verify(
                repo => repo.GetByKeyAsync(1, 5, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetReservationAsync_ReservationNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            ReservationKeyDto keyDto = new()
            {
                TripId = 999,
                Seat = 99
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.GetByKeyAsync(999, 99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reservation?)null);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _reservationService.GetReservationAsync(keyDto));

            Assert.Equal("No se encontró la reserva solicitada.", exception.Message);
        }

        [Fact]
        public async Task GetReservationAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            ReservationKeyDto keyDto = new()
            {
                TripId = 1,
                Seat = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.GetByKeyAsync(1, 5, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _reservationService.GetReservationAsync(keyDto));

            Assert.Equal("Error al consultar la reserva.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetReservationAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            ReservationKeyDto keyDto = new()
            {
                TripId = 1,
                Seat = 5
            };
            CancellationToken cancellationToken = new();
            Reservation expectedReservation = new() { TripId = 1, Seat = 5 };

            _ = _mockReservationRepository
                .Setup(repo => repo.GetByKeyAsync(1, 5, cancellationToken))
                .ReturnsAsync(expectedReservation);

            // Act
            _ = await _reservationService.GetReservationAsync(keyDto, cancellationToken);

            // Assert
            _mockReservationRepository.Verify(
                repo => repo.GetByKeyAsync(1, 5, cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetUserReservationsAsync Tests

        [Fact]
        public async Task GetUserReservationsAsync_ValidUserId_ReturnsUserReservations()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            List<UserReservationDetailsDto> expectedReservations =
            [
                new UserReservationDetailsDto
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
                    Seat = 5
                },
                new UserReservationDetailsDto
                {
                    TripId = 2,
                    DepartureStationId = 2,
                    DepartureStationName = "Station B",
                    DepartureCity = "City B",
                    DepartureProvince = "Province B",
                    DepartureCountry = "Country B",
                    DepartureTime = new DateTime(2024, 6, 2, 11, 0, 0),
                    ArrivalStationId = 3,
                    ArrivalStationName = "Station C",
                    ArrivalCity = "City C",
                    ArrivalProvince = "Province C",
                    ArrivalCountry = "Country C",
                    ArrivalTime = new DateTime(2024, 6, 2, 15, 0, 0),
                    Seat = 10
                }
            ];

            _ = _mockReservationRepository
                .Setup(repo => repo.GetByUserIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReservations);

            // Act
            IEnumerable<UserReservationDetailsDto> result = await _reservationService.GetUserReservationsAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            List<UserReservationDetailsDto> reservationList = [.. result];
            Assert.Equal(2, reservationList.Count);
            Assert.Equal(expectedReservations[0].TripId, reservationList[0].TripId);
            Assert.Equal(expectedReservations[0].Seat, reservationList[0].Seat);
            Assert.Equal(expectedReservations[1].TripId, reservationList[1].TripId);
            Assert.Equal(expectedReservations[1].Seat, reservationList[1].Seat);

            _mockReservationRepository.Verify(
                repo => repo.GetByUserIdWithDetailsAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetUserReservationsAsync_NoReservations_ReturnsEmptyCollection()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };

            _ = _mockReservationRepository
                .Setup(repo => repo.GetByUserIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IEnumerable<UserReservationDetailsDto> result = await _reservationService.GetUserReservationsAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockReservationRepository.Verify(
                repo => repo.GetByUserIdWithDetailsAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetUserReservationsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };

            _ = _mockReservationRepository
                .Setup(repo => repo.GetByUserIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _reservationService.GetUserReservationsAsync(keyDto));

            Assert.Equal("Error al obtener las reservas del usuario.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetUserReservationsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            CancellationToken cancellationToken = new();

            _ = _mockReservationRepository
                .Setup(repo => repo.GetByUserIdWithDetailsAsync(1, cancellationToken))
                .ReturnsAsync([]);

            // Act
            _ = await _reservationService.GetUserReservationsAsync(keyDto, cancellationToken);

            // Assert
            _mockReservationRepository.Verify(
                repo => repo.GetByUserIdWithDetailsAsync(1, cancellationToken),
                Times.Once);
        }

        #endregion

        #region SearchReservationsAsync Tests

        [Fact]
        public async Task SearchReservationsAsync_ValidRequest_ReturnsPagedResult()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() {
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
                        UserId = 1,
                        UserName = "user1",
                        Email = "user1@example.com",
                        Seat = 5
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.TotalCount, result.TotalCount);
            Assert.Equal(expectedResult.Page, result.Page);
            Assert.Equal(expectedResult.PageSize, result.PageSize);
            _ = Assert.Single(result.Items);
            ReservationDetailsDto firstItem = result.Items.First();
            Assert.Equal(expectedResult.Items.First().UserId, firstItem.UserId);
            Assert.Equal(expectedResult.Items.First().UserName, firstItem.UserName);

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_NoResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithFilterEquals_ReturnsFilteredResults()
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

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, UserId = 1, Seat = 5 }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, item => Assert.Equal(1, item.TripId));

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithFilterContains_ReturnsFilteredResults()
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
                        new Filter("UserName", FilterOperator.Contains, "john")
                    ])
                ],
                Orders = []
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "johnsmith", TripId = 1, Seat = 5 },
                    new() { UserId = 2, UserName = "johndoe", TripId = 2, Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.Contains("john", item.UserName.ToLower()));

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithFilterGreaterThan_ReturnsFilteredResults()
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
                        new Filter("Seat", FilterOperator.GreaterThan, 5)
                    ])
                ],
                Orders = []
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 10 },
                    new() { TripId = 2, Seat = 15 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.Seat > 5));

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithFilterBetween_ReturnsFilteredResults()
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

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureTime = new DateTime(2024, 6, 5), Seat = 5 },
                    new() { TripId = 2, DepartureTime = new DateTime(2024, 6, 15), Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.DepartureTime >= new DateTime(2024, 6, 1));
                Assert.True(item.DepartureTime <= new DateTime(2024, 6, 30));
            });

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithMultipleFiltersInSameClause_ReturnsFilteredResults()
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
                        new Filter("TripId", FilterOperator.Equals, 1),
                        new Filter("TripId", FilterOperator.Equals, 2)
                    ])
                ],
                Orders = []
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.TripId == 1 || item.TripId == 2));

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithMultipleFilterClauses_ReturnsFilteredResults()
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
                        new Filter("TripId", FilterOperator.GreaterThan, 0)
                    ]),
                    new FilterClause(
                    [
                        new Filter("Seat", FilterOperator.LessThan, 20)
                    ])
                ],
                Orders = []
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.TripId > 0);
                Assert.True(item.Seat < 20);
            });

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithOrderByAscending_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("Seat", SortDirection.Asc)
                ]
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 10 },
                    new() { TripId = 3, Seat = 15 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<ReservationDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].Seat >= items[i - 1].Seat);
            }

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithOrderByDescending_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("Seat", SortDirection.Desc)
                ]
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 3, Seat = 15 },
                    new() { TripId = 2, Seat = 10 },
                    new() { TripId = 1, Seat = 5 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<ReservationDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].Seat <= items[i - 1].Seat);
            }

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithMultipleOrders_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("TripId", SortDirection.Asc),
                    new SortOption("Seat", SortDirection.Desc)
                ]
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 10 },
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 15 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<ReservationDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].TripId >= items[i - 1].TripId);
                if (items[i].TripId == items[i - 1].TripId)
                {
                    Assert.True(items[i].Seat <= items[i - 1].Seat);
                }
            }

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithFiltersAndOrders_ReturnsFilteredAndOrderedResults()
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
                        new Filter("TripId", FilterOperator.LessThanOrEqual, 10)
                    ])
                ],
                Orders =
                [
                    new SortOption("Seat", SortDirection.Asc)
                ]
            };

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 5, Seat = 5 },
                    new() { TripId = 3, Seat = 10 },
                    new() { TripId = 8, Seat = 15 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<ReservationDetailsDto> result = await _reservationService.SearchReservationsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.TripId <= 10));
            List<ReservationDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].Seat >= items[i - 1].Seat);
            }

            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservationsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _reservationService.SearchReservationsAsync(searchDto));

            Assert.Equal("Error al buscar reservas.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task SearchReservationsAsync_WithCancellationToken_PassesTokenToRepository()
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
            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchWithDetailsAsync(searchDto, cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            _ = await _reservationService.SearchReservationsAsync(searchDto, cancellationToken);

            // Assert
            _mockReservationRepository.Verify(
                repo => repo.SearchWithDetailsAsync(searchDto, cancellationToken),
                Times.Once);
        }

        #endregion

        #region SearchUserReservationsAsync Tests

        [Fact]
        public async Task SearchUserReservationsAsync_ValidRequest_ReturnsPagedResult()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() {
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
                        Seat = 5
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.TotalCount, result.TotalCount);
            Assert.Equal(expectedResult.Page, result.Page);
            Assert.Equal(expectedResult.PageSize, result.PageSize);
            _ = Assert.Single(result.Items);
            UserReservationDetailsDto firstItem = result.Items.First();
            Assert.Equal(expectedResult.Items.First().TripId, firstItem.TripId);
            Assert.Equal(expectedResult.Items.First().Seat, firstItem.Seat);

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_NoResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_MultipleReservations_ReturnsCorrectPagedResult()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 5,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 10 },
                    new() { TripId = 3, Seat = 15 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 5
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count());

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithFilterEquals_ReturnsFilteredResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("TripId", FilterOperator.Equals, 2)
                    ])
                ],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 2, Seat = 10 }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, item => Assert.Equal(2, item.TripId));

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithFilterContains_ReturnsFilteredResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureCity", FilterOperator.Contains, "City")
                    ])
                ],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureCity = "City A", Seat = 5 },
                    new() { TripId = 2, DepartureCity = "City B", Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.Contains("City", item.DepartureCity));

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithFilterStartsWith_ReturnsFilteredResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("DepartureStationName", FilterOperator.StartsWith, "Station")
                    ])
                ],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureStationName = "Station A", Seat = 5 },
                    new() { TripId = 2, DepartureStationName = "Station B", Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.StartsWith("Station", item.DepartureStationName));

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithFilterGreaterThanOrEqual_ReturnsFilteredResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("Seat", FilterOperator.GreaterThanOrEqual, 10)
                    ])
                ],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 2, Seat = 10 },
                    new() { TripId = 3, Seat = 15 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.Seat >= 10));

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithFilterBetween_ReturnsFilteredResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
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

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, DepartureTime = new DateTime(2024, 6, 5), Seat = 5 },
                    new() { TripId = 2, DepartureTime = new DateTime(2024, 6, 20), Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.DepartureTime >= new DateTime(2024, 6, 1));
                Assert.True(item.DepartureTime <= new DateTime(2024, 6, 30));
            });

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithFilterNotEquals_ReturnsFilteredResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
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

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 2, DepartureStationId = 2, Seat = 10 },
                    new() { TripId = 3, DepartureStationId = 3, Seat = 15 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.NotEqual(1, item.DepartureStationId));

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithMultipleFiltersInSameClause_ReturnsFilteredResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("TripId", FilterOperator.Equals, 1),
                        new Filter("TripId", FilterOperator.Equals, 2)
                    ])
                ],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.TripId == 1 || item.TripId == 2));

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithMultipleFilterClauses_ReturnsFilteredResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("TripId", FilterOperator.GreaterThan, 0)
                    ]),
                    new FilterClause(
                    [
                        new Filter("Seat", FilterOperator.LessThan, 20)
                    ])
                ],
                Orders = []
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 10 }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.TripId > 0);
                Assert.True(item.Seat < 20);
            });

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithOrderByAscending_ReturnsOrderedResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("Seat", SortDirection.Asc)
                ]
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 10 },
                    new() { TripId = 3, Seat = 15 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<UserReservationDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].Seat >= items[i - 1].Seat);
            }

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithOrderByDescending_ReturnsOrderedResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("DepartureTime", SortDirection.Desc)
                ]
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 3, DepartureTime = new DateTime(2024, 6, 15), Seat = 15 },
                    new() { TripId = 2, DepartureTime = new DateTime(2024, 6, 10), Seat = 10 },
                    new() { TripId = 1, DepartureTime = new DateTime(2024, 6, 5), Seat = 5 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<UserReservationDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].DepartureTime <= items[i - 1].DepartureTime);
            }

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithMultipleOrders_ReturnsOrderedResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("TripId", SortDirection.Asc),
                    new SortOption("Seat", SortDirection.Desc)
                ]
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 10 },
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 15 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<UserReservationDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].TripId >= items[i - 1].TripId);
                if (items[i].TripId == items[i - 1].TripId)
                {
                    Assert.True(items[i].Seat <= items[i - 1].Seat);
                }
            }

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithFiltersAndOrders_ReturnsFilteredAndOrderedResults()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses =
                [
                    new FilterClause(
                    [
                        new Filter("Seat", FilterOperator.LessThanOrEqual, 20)
                    ])
                ],
                Orders =
                [
                    new SortOption("Seat", SortDirection.Asc)
                ]
            };

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1, Seat = 5 },
                    new() { TripId = 2, Seat = 10 },
                    new() { TripId = 3, Seat = 15 }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserReservationDetailsDto> result = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.Seat <= 20));
            List<UserReservationDetailsDto> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].Seat >= items[i - 1].Seat);
            }

            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _reservationService.SearchUserReservationsAsync(keyDto, searchDto));

            Assert.Equal("Error al buscar reservas.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task SearchUserReservationsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };
            CancellationToken cancellationToken = new();
            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockReservationRepository
                .Setup(repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            _ = await _reservationService.SearchUserReservationsAsync(keyDto, searchDto, cancellationToken);

            // Assert
            _mockReservationRepository.Verify(
                repo => repo.SearchForUserIdWithDetailsAsync(1, searchDto, cancellationToken),
                Times.Once);
        }

        #endregion
    }
}
