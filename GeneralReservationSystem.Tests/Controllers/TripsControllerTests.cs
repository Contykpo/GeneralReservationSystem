using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.API.Controllers;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GeneralReservationSystem.Tests.Controllers
{
    public class TripsControllerTests
    {
        private readonly Mock<ITripService> _mockTripService;
        private readonly Mock<IValidator<PagedSearchRequestDto>> _mockPagedSearchValidator;
        private readonly Mock<IValidator<CreateTripDto>> _mockCreateTripValidator;
        private readonly Mock<IValidator<UpdateTripDto>> _mockUpdateTripValidator;
        private readonly Mock<IValidator<TripKeyDto>> _mockTripKeyValidator;
        private readonly TripsController _controller;

        public TripsControllerTests()
        {
            _mockTripService = new Mock<ITripService>();
            _mockPagedSearchValidator = new Mock<IValidator<PagedSearchRequestDto>>();
            _mockCreateTripValidator = new Mock<IValidator<CreateTripDto>>();
            _mockUpdateTripValidator = new Mock<IValidator<UpdateTripDto>>();
            _mockTripKeyValidator = new Mock<IValidator<TripKeyDto>>();

            _controller = new TripsController(
                _mockTripService.Object,
                _mockPagedSearchValidator.Object,
                _mockCreateTripValidator.Object,
                _mockUpdateTripValidator.Object,
                _mockTripKeyValidator.Object);

            // Setup validators to return valid by default
            _ = _mockPagedSearchValidator
                .Setup(v => v.ValidateAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockCreateTripValidator
                .Setup(v => v.ValidateAsync(It.IsAny<CreateTripDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockUpdateTripValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateTripDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockTripKeyValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TripKeyDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        private void SetupAdminUser()
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            ];

            ClaimsIdentity identity = new(claims, "TestAuth");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        #region GetAllTrips Tests

        [Fact]
        public async Task GetAllTrips_ReturnsOkWithTrips()
        {
            // Arrange
            List<Trip> expectedTrips =
            [
                new Trip { TripId = 1, DepartureStationId = 1, ArrivalStationId = 2, AvailableSeats = 50 },
                new Trip { TripId = 2, DepartureStationId = 2, ArrivalStationId = 3, AvailableSeats = 40 }
            ];

            _ = _mockTripService
                .Setup(s => s.GetAllTripsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTrips);

            // Act
            IActionResult result = await _controller.GetAllTrips(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            IEnumerable<Trip> trips = Assert.IsType<IEnumerable<Trip>>(okResult.Value, exactMatch: false);
            Assert.Equal(2, trips.Count());

            _mockTripService.Verify(
                s => s.GetAllTripsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllTrips_NoTrips_ReturnsOkWithEmptyList()
        {
            // Arrange
            _ = _mockTripService
                .Setup(s => s.GetAllTripsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetAllTrips(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            IEnumerable<Trip> trips = Assert.IsType<IEnumerable<Trip>>(okResult.Value, exactMatch: false);
            Assert.Empty(trips);
        }

        #endregion

        #region SearchTrips (POST) Tests

        [Fact]
        public async Task SearchTrips_Post_ReturnsOkWithPagedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                Filters = [],
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

            _ = _mockTripService
                .Setup(s => s.SearchTripsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            IActionResult result = await _controller.SearchTrips(searchDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<TripWithDetailsDto> pagedResult = Assert.IsType<PagedResult<TripWithDetailsDto>>(okResult.Value);
            _ = Assert.Single(pagedResult.Items);
            Assert.Equal(1, pagedResult.TotalCount);
        }

        [Fact]
        public async Task SearchTrips_Post_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 0, // Invalid
                PageSize = 10
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("Page", "Page must be greater than 0")
            ];

            _ = _mockPagedSearchValidator
                .Setup(v => v.ValidateAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.SearchTrips(searchDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockTripService.Verify(
                s => s.SearchTripsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region SearchTrips (GET) Tests

        [Fact]
        public async Task SearchTrips_Get_ReturnsOkWithPagedResults()
        {
            // Arrange
            PagedResult<TripWithDetailsDto> expectedResult = new()
            {
                Items =
                [
                    new() { TripId = 1 }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockTripService
                .Setup(s => s.SearchTripsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=10");

            // Act
            IActionResult result = await _controller.SearchTrips(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<TripWithDetailsDto> pagedResult = Assert.IsType<PagedResult<TripWithDetailsDto>>(okResult.Value);
            _ = Assert.Single(pagedResult.Items);
        }

        #endregion

        #region GetTrip Tests

        [Fact]
        public async Task GetTrip_ValidId_ReturnsOkWithTrip()
        {
            // Arrange
            Trip expectedTrip = new()
            {
                TripId = 1,
                DepartureStationId = 1,
                ArrivalStationId = 2,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripService
                .Setup(s => s.GetTripAsync(It.Is<TripKeyDto>(k => k.TripId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTrip);

            // Act
            IActionResult result = await _controller.GetTrip(1, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Trip trip = Assert.IsType<Trip>(okResult.Value);
            Assert.Equal(expectedTrip.TripId, trip.TripId);
        }

        [Fact]
        public async Task GetTrip_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            _ = _mockTripService
                .Setup(s => s.GetTripAsync(It.Is<TripKeyDto>(k => k.TripId == 999), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Trip not found"));

            // Act
            IActionResult result = await _controller.GetTrip(999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetTrip_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("TripId", "Invalid trip ID")
            ];

            _ = _mockTripKeyValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TripKeyDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.GetTrip(0, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetTripWithDetails Tests

        [Fact]
        public async Task GetTripWithDetails_ValidId_ReturnsOkWithTripDetails()
        {
            // Arrange
            TripWithDetailsDto expectedTrip = new()
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

            _ = _mockTripService
                .Setup(s => s.GetTripWithDetailsAsync(It.Is<TripKeyDto>(k => k.TripId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTrip);

            // Act
            IActionResult result = await _controller.GetTripWithDetails(1, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TripWithDetailsDto trip = Assert.IsType<TripWithDetailsDto>(okResult.Value);
            Assert.Equal(expectedTrip.TripId, trip.TripId);
            Assert.Equal(expectedTrip.DepartureStationName, trip.DepartureStationName);
        }

        [Fact]
        public async Task GetTripWithDetails_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            _ = _mockTripService
                .Setup(s => s.GetTripWithDetailsAsync(It.Is<TripKeyDto>(k => k.TripId == 999), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Trip not found"));

            // Act
            IActionResult result = await _controller.GetTripWithDetails(999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region CreateTrip Tests

        [Fact]
        public async Task CreateTrip_ValidDto_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupAdminUser();

            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 10, 0, 0),
                ArrivalStationId = 2,
                ArrivalTime = new DateTime(2024, 6, 1, 14, 0, 0),
                AvailableSeats = 50
            };

            Trip createdTrip = new()
            {
                TripId = 1,
                DepartureStationId = createDto.DepartureStationId,
                DepartureTime = createDto.DepartureTime,
                ArrivalStationId = createDto.ArrivalStationId,
                ArrivalTime = createDto.ArrivalTime,
                AvailableSeats = createDto.AvailableSeats
            };

            _ = _mockTripService
                .Setup(s => s.CreateTripAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTrip);

            // Act
            IActionResult result = await _controller.CreateTrip(createDto, CancellationToken.None);

            // Assert
            CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetTrip), createdResult.ActionName);
            Trip trip = Assert.IsType<Trip>(createdResult.Value);
            Assert.Equal(createdTrip.TripId, trip.TripId);
        }

        [Fact]
        public async Task CreateTrip_BusinessRuleViolation_ReturnsConflict()
        {
            // Arrange
            SetupAdminUser();

            CreateTripDto createDto = new()
            {
                DepartureStationId = 1,
                DepartureTime = new DateTime(2024, 6, 1, 14, 0, 0),
                ArrivalStationId = 1, // Same as departure
                ArrivalTime = new DateTime(2024, 6, 1, 10, 0, 0),
                AvailableSeats = 50
            };

            _ = _mockTripService
                .Setup(s => s.CreateTripAsync(createDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Departure and arrival stations must be different"));

            // Act
            IActionResult result = await _controller.CreateTrip(createDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateTrip_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            CreateTripDto createDto = new()
            {
                AvailableSeats = -5 // Invalid
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("AvailableSeats", "Must be positive")
            ];

            _ = _mockCreateTripValidator
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.CreateTrip(createDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockTripService.Verify(
                s => s.CreateTripAsync(It.IsAny<CreateTripDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region UpdateTrip Tests

        [Fact]
        public async Task UpdateTrip_ValidDto_ReturnsOkWithUpdatedTrip()
        {
            // Arrange
            SetupAdminUser();

            UpdateTripDto updateDto = new()
            {
                AvailableSeats = 60
            };

            Trip updatedTrip = new()
            {
                TripId = 1,
                DepartureStationId = 1,
                ArrivalStationId = 2,
                AvailableSeats = 60
            };

            _ = _mockTripService
                .Setup(s => s.UpdateTripAsync(It.Is<UpdateTripDto>(d => d.TripId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedTrip);

            // Act
            IActionResult result = await _controller.UpdateTrip(1, updateDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Trip trip = Assert.IsType<Trip>(okResult.Value);
            Assert.Equal(updatedTrip.TripId, trip.TripId);
            Assert.Equal(60, trip.AvailableSeats);
        }

        [Fact]
        public async Task UpdateTrip_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            UpdateTripDto updateDto = new()
            {
                AvailableSeats = 60
            };

            _ = _mockTripService
                .Setup(s => s.UpdateTripAsync(It.IsAny<UpdateTripDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Trip not found"));

            // Act
            IActionResult result = await _controller.UpdateTrip(999, updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateTrip_BusinessRuleViolation_ReturnsConflict()
        {
            // Arrange
            SetupAdminUser();

            UpdateTripDto updateDto = new()
            {
                DepartureStationId = 1,
                ArrivalStationId = 1 // Same as departure
            };

            _ = _mockTripService
                .Setup(s => s.UpdateTripAsync(It.IsAny<UpdateTripDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Departure and arrival stations must be different"));

            // Act
            IActionResult result = await _controller.UpdateTrip(1, updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        #endregion

        #region DeleteTrip Tests

        [Fact]
        public async Task DeleteTrip_ValidId_ReturnsNoContent()
        {
            // Arrange
            SetupAdminUser();

            _ = _mockTripService
                .Setup(s => s.DeleteTripAsync(It.Is<TripKeyDto>(k => k.TripId == 1), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.DeleteTrip(1, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NoContentResult>(result);

            _mockTripService.Verify(
                s => s.DeleteTripAsync(It.Is<TripKeyDto>(k => k.TripId == 1), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteTrip_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            _ = _mockTripService
                .Setup(s => s.DeleteTripAsync(It.IsAny<TripKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Trip not found"));

            // Act
            IActionResult result = await _controller.DeleteTrip(999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetFreeSeats Tests

        [Fact]
        public async Task GetFreeSeats_ValidTripId_ReturnsOkWithSeatList()
        {
            // Arrange
            List<int> expectedSeats = [1, 2, 3, 5, 7, 9, 10];

            _ = _mockTripService
                .Setup(s => s.GetFreeSeatsAsync(It.Is<TripKeyDto>(k => k.TripId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSeats);

            // Act
            IActionResult result = await _controller.GetFreeSeats(1, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            IEnumerable<int> seats = Assert.IsType<IEnumerable<int>>(okResult.Value, exactMatch: false);
            Assert.Equal(expectedSeats.Count, seats.Count());
        }

        [Fact]
        public async Task GetFreeSeats_NoFreeSeats_ReturnsOkWithEmptyList()
        {
            // Arrange
            _ = _mockTripService
                .Setup(s => s.GetFreeSeatsAsync(It.Is<TripKeyDto>(k => k.TripId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetFreeSeats(1, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            IEnumerable<int> seats = Assert.IsType<IEnumerable<int>>(okResult.Value, exactMatch: false);
            Assert.Empty(seats);
        }

        [Fact]
        public async Task GetFreeSeats_TripNotFound_ReturnsNotFound()
        {
            // Arrange
            _ = _mockTripService
                .Setup(s => s.GetFreeSeatsAsync(It.Is<TripKeyDto>(k => k.TripId == 999), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Trip not found"));

            // Act
            IActionResult result = await _controller.GetFreeSeats(999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetFreeSeats_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            _ = _mockTripService
                .Setup(s => s.GetFreeSeatsAsync(It.Is<TripKeyDto>(k => k.TripId == 1), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceException("Database error"));

            // Act
            IActionResult result = await _controller.GetFreeSeats(1, CancellationToken.None);

            // Assert
            ObjectResult statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion
    }
}
