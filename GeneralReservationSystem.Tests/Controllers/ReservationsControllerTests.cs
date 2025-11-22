using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.API.Controllers;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Security.Claims;

namespace GeneralReservationSystem.Tests.Controllers
{
    public class ReservationsControllerTests
    {
        private readonly Mock<IReservationService> _mockReservationService;
        private readonly Mock<IValidator<PagedSearchRequestDto>> _mockPagedSearchValidator;
        private readonly Mock<IValidator<CreateReservationDto>> _mockCreateReservationValidator;
        private readonly Mock<IValidator<ReservationKeyDto>> _mockReservationKeyValidator;
        private readonly Mock<IValidator<UserKeyDto>> _mockUserKeyValidator;
        private readonly ReservationsController _controller;

        public ReservationsControllerTests()
        {
            _mockReservationService = new Mock<IReservationService>();
            _mockPagedSearchValidator = new Mock<IValidator<PagedSearchRequestDto>>();
            _mockCreateReservationValidator = new Mock<IValidator<CreateReservationDto>>();
            _mockReservationKeyValidator = new Mock<IValidator<ReservationKeyDto>>();
            _mockUserKeyValidator = new Mock<IValidator<UserKeyDto>>();

            _controller = new ReservationsController(
                _mockReservationService.Object,
                _mockPagedSearchValidator.Object,
                _mockCreateReservationValidator.Object,
                _mockReservationKeyValidator.Object,
                _mockUserKeyValidator.Object);

            // Setup validators to return valid by default
            _ = _mockPagedSearchValidator
                .Setup(v => v.ValidateAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockCreateReservationValidator
                .Setup(v => v.ValidateAsync(It.IsAny<CreateReservationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockReservationKeyValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ReservationKeyDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockUserKeyValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        private void SetupAuthenticatedUser(int userId, bool isAdmin = false)
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, $"user{userId}")
            ];

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

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

        #region SearchReservations Tests

        [Fact]
        public async Task SearchReservations_NoFiltersOrOrders_ReturnsPagedResult()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<ReservationDetailsDto> reservations =
            [
                new ReservationDetailsDto
                {
                    TripId = 1,
                    UserId = 1,
                    UserName = "user1",
                    Email = "user1@example.com",
                    Seat = 5,
                    DepartureStationName = "Station A",
                    ArrivalStationName = "Station B"
                },
                new ReservationDetailsDto
                {
                    TripId = 2,
                    UserId = 2,
                    UserName = "user2",
                    Email = "user2@example.com",
                    Seat = 10,
                    DepartureStationName = "Station C",
                    ArrivalStationName = "Station D"
                }
            ];

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 2,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchReservationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<ReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<ReservationDetailsDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.Items.Count());

            _mockReservationService.Verify(
                s => s.SearchReservationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchReservations_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<ReservationDetailsDto> reservations =
            [
                new ReservationDetailsDto
                {
                    TripId = 1,
                    UserId = 1,
                    Seat = 5
                }
            ];

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 10,
                Page = 2,
                PageSize = 5
            };

            _ = _mockReservationService
                .Setup(s => s.SearchReservationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "page", new StringValues("2") },
                { "pageSize", new StringValues("5") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<ReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<ReservationDetailsDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.Page);
            Assert.Equal(5, pagedResult.PageSize);
            Assert.Equal(10, pagedResult.TotalCount);
        }

        [Fact]
        public async Task SearchReservations_WithFilters_ReturnsFilteredResults()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<ReservationDetailsDto> reservations =
            [
                new ReservationDetailsDto
                {
                    TripId = 1,
                    UserId = 1,
                    UserName = "testuser",
                    Seat = 5
                }
            ];

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchReservationsAsync(It.Is<PagedSearchRequestDto>(dto =>
                    dto.FilterClauses.Any()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "filters", new StringValues("[TripId|Equals|1]") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<ReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<ReservationDetailsDto>>(okResult.Value);
            Assert.Single(pagedResult.Items);
        }

        [Fact]
        public async Task SearchReservations_WithOrders_ReturnsSortedResults()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<ReservationDetailsDto> reservations =
            [
                new ReservationDetailsDto
                {
                    TripId = 1,
                    UserId = 1,
                    Seat = 10
                },
                new ReservationDetailsDto
                {
                    TripId = 1,
                    UserId = 2,
                    Seat = 5
                }
            ];

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 2,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchReservationsAsync(It.Is<PagedSearchRequestDto>(dto =>
                    dto.Orders.Any()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "orders", new StringValues("Seat|Desc") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<ReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<ReservationDetailsDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.Items.Count());
        }

        [Fact]
        public async Task SearchReservations_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("Page", "Page must be greater than 0")
            ];

            _ = _mockPagedSearchValidator
                .Setup(v => v.ValidateAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "page", new StringValues("0") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchReservations(CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockReservationService.Verify(
                s => s.SearchReservationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SearchReservations_EmptyResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            PagedResult<ReservationDetailsDto> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchReservationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<ReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<ReservationDetailsDto>>(okResult.Value);
            Assert.Empty(pagedResult.Items);
            Assert.Equal(0, pagedResult.TotalCount);
        }

        [Fact]
        public async Task SearchReservations_ServiceError_ThrowsServiceException()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            _ = _mockReservationService
                .Setup(s => s.SearchReservationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceException("Database error"));

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceException>(
                () => _controller.SearchReservations(CancellationToken.None));
        }

        #endregion

        #region SearchUserReservations Tests

        [Fact]
        public async Task SearchUserReservations_AsOwner_ReturnsPagedResult()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            List<UserReservationDetailsDto> reservations =
            [
                new UserReservationDetailsDto
                {
                    TripId = 1,
                    Seat = 5,
                    DepartureStationName = "Station A",
                    ArrivalStationName = "Station B"
                }
            ];

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchUserReservationsAsync(It.Is<UserKeyDto>(k => k.UserId == 1), It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchUserReservations(1, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<UserReservationDetailsDto>>(okResult.Value);
            Assert.Single(pagedResult.Items);
        }

        [Fact]
        public async Task SearchUserReservations_AsAdmin_ReturnsPagedResult()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<UserReservationDetailsDto> reservations =
            [
                new UserReservationDetailsDto
                {
                    TripId = 1,
                    Seat = 5
                }
            ];

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchUserReservationsAsync(It.Is<UserKeyDto>(k => k.UserId == 2), It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchUserReservations(2, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<UserReservationDetailsDto>>(okResult.Value);
            Assert.Single(pagedResult.Items);
        }

        [Fact]
        public async Task SearchUserReservations_AsNonAdminForOtherUser_ReturnsForbid()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchUserReservations(2, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ForbidResult>(result);

            _mockReservationService.Verify(
                s => s.SearchUserReservationsAsync(It.IsAny<UserKeyDto>(), It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SearchUserReservations_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            List<UserReservationDetailsDto> reservations =
            [
                new UserReservationDetailsDto
                {
                    TripId = 1,
                    Seat = 5
                }
            ];

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 10,
                Page = 2,
                PageSize = 5
            };

            _ = _mockReservationService
                .Setup(s => s.SearchUserReservationsAsync(It.IsAny<UserKeyDto>(), It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "page", new StringValues("2") },
                { "pageSize", new StringValues("5") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchUserReservations(1, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<UserReservationDetailsDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.Page);
            Assert.Equal(5, pagedResult.PageSize);
        }

        [Fact]
        public async Task SearchUserReservations_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            ClaimsIdentity identity = new();
            ClaimsPrincipal claimsPrincipal = new(identity);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchUserReservations(1, CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region SearchCurrentUserReservations Tests

        [Fact]
        public async Task SearchCurrentUserReservations_ValidUser_ReturnsPagedResult()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            List<UserReservationDetailsDto> reservations =
            [
                new UserReservationDetailsDto
                {
                    TripId = 1,
                    Seat = 5,
                    DepartureStationName = "Station A",
                    ArrivalStationName = "Station B"
                },
                new UserReservationDetailsDto
                {
                    TripId = 2,
                    Seat = 10,
                    DepartureStationName = "Station C",
                    ArrivalStationName = "Station D"
                }
            ];

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 2,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchUserReservationsAsync(It.Is<UserKeyDto>(k => k.UserId == 1), It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchCurrentUserReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<UserReservationDetailsDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.Items.Count());
        }

        [Fact]
        public async Task SearchCurrentUserReservations_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            List<UserReservationDetailsDto> reservations =
            [
                new UserReservationDetailsDto
                {
                    TripId = 1,
                    Seat = 5
                }
            ];

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 10,
                Page = 2,
                PageSize = 5
            };

            _ = _mockReservationService
                .Setup(s => s.SearchUserReservationsAsync(It.IsAny<UserKeyDto>(), It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "page", new StringValues("2") },
                { "pageSize", new StringValues("5") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchCurrentUserReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<UserReservationDetailsDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.Page);
            Assert.Equal(5, pagedResult.PageSize);
            Assert.Equal(10, pagedResult.TotalCount);
        }

        [Fact]
        public async Task SearchCurrentUserReservations_WithFilters_ReturnsFilteredResults()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            List<UserReservationDetailsDto> reservations =
            [
                new UserReservationDetailsDto
                {
                    TripId = 1,
                    Seat = 5
                }
            ];

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchUserReservationsAsync(It.IsAny<UserKeyDto>(), It.Is<PagedSearchRequestDto>(dto =>
                    dto.FilterClauses.Any()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "filters", new StringValues("[TripId|Equals|1]") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchCurrentUserReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<UserReservationDetailsDto>>(okResult.Value);
            Assert.Single(pagedResult.Items);
        }

        [Fact]
        public async Task SearchCurrentUserReservations_WithOrders_ReturnsSortedResults()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            List<UserReservationDetailsDto> reservations =
            [
                new UserReservationDetailsDto
                {
                    TripId = 1,
                    Seat = 10
                },
                new UserReservationDetailsDto
                {
                    TripId = 2,
                    Seat = 5
                }
            ];

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = reservations,
                TotalCount = 2,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchUserReservationsAsync(It.IsAny<UserKeyDto>(), It.Is<PagedSearchRequestDto>(dto =>
                    dto.Orders.Any()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "orders", new StringValues("Seat|Desc") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchCurrentUserReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<UserReservationDetailsDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.Items.Count());
        }

        [Fact]
        public async Task SearchCurrentUserReservations_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("Page", "Page must be greater than 0")
            ];

            _ = _mockPagedSearchValidator
                .Setup(v => v.ValidateAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "page", new StringValues("0") }
            });
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchCurrentUserReservations(CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockReservationService.Verify(
                s => s.SearchUserReservationsAsync(It.IsAny<UserKeyDto>(), It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SearchCurrentUserReservations_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            ClaimsIdentity identity = new();
            ClaimsPrincipal claimsPrincipal = new(identity);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchCurrentUserReservations(CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SearchCurrentUserReservations_EmptyResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            PagedResult<UserReservationDetailsDto> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            };

            _ = _mockReservationService
                .Setup(s => s.SearchUserReservationsAsync(It.IsAny<UserKeyDto>(), It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.User,
                    Request = { Query = queryCollection }
                }
            };

            // Act
            IActionResult result = await _controller.SearchCurrentUserReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserReservationDetailsDto> pagedResult = Assert.IsType<PagedResult<UserReservationDetailsDto>>(okResult.Value);
            Assert.Empty(pagedResult.Items);
            Assert.Equal(0, pagedResult.TotalCount);
        }

        #endregion

        #region GetMyReservations Tests

        [Fact]
        public async Task GetMyReservations_ValidUser_ReturnsOkWithReservations()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            List<UserReservationDetailsDto> expectedReservations =
            [
                new UserReservationDetailsDto { TripId = 1, Seat = 5 },
                new UserReservationDetailsDto { TripId = 2, Seat = 10 }
            ];

            _ = _mockReservationService
                .Setup(s => s.GetUserReservationsAsync(It.Is<UserKeyDto>(k => k.UserId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReservations);

            // Act
            IActionResult result = await _controller.GetMyReservations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            IEnumerable<UserReservationDetailsDto> reservations = Assert.IsType<IEnumerable<UserReservationDetailsDto>>(okResult.Value, exactMatch: false);
            Assert.Equal(2, reservations.Count());
        }

        [Fact]
        public async Task GetMyReservations_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            ClaimsIdentity identity = new();
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            // Act
            IActionResult result = await _controller.GetMyReservations(CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region GetUserReservations Tests

        [Fact]
        public async Task GetUserReservations_AsAdmin_ReturnsOkWithReservations()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<UserReservationDetailsDto> expectedReservations =
            [
                new UserReservationDetailsDto { TripId = 1, Seat = 5 }
            ];

            _ = _mockReservationService
                .Setup(s => s.GetUserReservationsAsync(It.Is<UserKeyDto>(k => k.UserId == 2), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReservations);

            // Act
            IActionResult result = await _controller.GetUserReservations(2, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            IEnumerable<UserReservationDetailsDto> reservations = Assert.IsType<IEnumerable<UserReservationDetailsDto>>(okResult.Value, exactMatch: false);
            _ = Assert.Single(reservations);
        }

        #endregion

        #region CreateReservation Tests

        [Fact]
        public async Task CreateReservation_AsOwnUser_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            Reservation createdReservation = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.CreateReservationAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdReservation);

            // Act
            IActionResult result = await _controller.CreateReservation(createDto, CancellationToken.None);

            // Assert
            CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetReservation), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateReservation_AsAdmin_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 2, // Different user
                Seat = 5
            };

            Reservation createdReservation = new()
            {
                TripId = 1,
                UserId = 2,
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.CreateReservationAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdReservation);

            // Act
            IActionResult result = await _controller.CreateReservation(createDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task CreateReservation_AsNonAdminForOtherUser_ReturnsForbid()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 2, // Different user
                Seat = 5
            };

            // Act
            IActionResult result = await _controller.CreateReservation(createDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ForbidResult>(result);

            _mockReservationService.Verify(
                s => s.CreateReservationAsync(It.IsAny<CreateReservationDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateReservation_SeatAlreadyTaken_ReturnsConflict()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.CreateReservationAsync(createDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Seat already reserved"));

            // Act
            IActionResult result = await _controller.CreateReservation(createDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateReservation_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            ClaimsIdentity identity = new();
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            CreateReservationDto createDto = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            // Act
            IActionResult result = await _controller.CreateReservation(createDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region CreateReservationForMyself Tests

        [Fact]
        public async Task CreateReservationForMyself_ValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            ReservationKeyDto keyDto = new()
            {
                TripId = 1,
                Seat = 5
            };

            Reservation createdReservation = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.CreateReservationAsync(
                    It.Is<CreateReservationDto>(d => d.TripId == 1 && d.Seat == 5 && d.UserId == 1),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdReservation);

            // Act
            IActionResult result = await _controller.CreateReservationForMyself(keyDto, CancellationToken.None);

            // Assert
            CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetReservation), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateReservationForMyself_SeatAlreadyTaken_ReturnsConflict()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            ReservationKeyDto keyDto = new()
            {
                TripId = 1,
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.CreateReservationAsync(It.IsAny<CreateReservationDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Seat already reserved"));

            // Act
            IActionResult result = await _controller.CreateReservationForMyself(keyDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        #endregion

        #region GetReservation Tests

        [Fact]
        public async Task GetReservation_AsOwner_ReturnsOkWithReservation()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            Reservation reservation = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.GetReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

            // Act
            IActionResult result = await _controller.GetReservation(1, 5, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Reservation returnedReservation = Assert.IsType<Reservation>(okResult.Value);
            Assert.Equal(reservation.TripId, returnedReservation.TripId);
        }

        [Fact]
        public async Task GetReservation_AsAdmin_ReturnsOkWithReservation()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            Reservation reservation = new()
            {
                TripId = 1,
                UserId = 2, // Different user
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.GetReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

            // Act
            IActionResult result = await _controller.GetReservation(1, 5, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            _ = Assert.IsType<Reservation>(okResult.Value);
        }

        [Fact]
        public async Task GetReservation_AsNonAdminForOtherUser_ReturnsForbid()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            Reservation reservation = new()
            {
                TripId = 1,
                UserId = 2, // Different user
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.GetReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

            // Act
            IActionResult result = await _controller.GetReservation(1, 5, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetReservation_ReservationNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            _ = _mockReservationService
                .Setup(s => s.GetReservationAsync(It.IsAny<ReservationKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Reservation not found"));

            // Act
            IActionResult result = await _controller.GetReservation(1, 999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region DeleteReservation Tests

        [Fact]
        public async Task DeleteReservation_AsOwner_ReturnsNoContent()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            Reservation reservation = new()
            {
                TripId = 1,
                UserId = 1,
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.GetReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

            _ = _mockReservationService
                .Setup(s => s.DeleteReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.DeleteReservation(1, 5, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NoContentResult>(result);

            _mockReservationService.Verify(
                s => s.DeleteReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteReservation_AsAdmin_ReturnsNoContent()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            Reservation reservation = new()
            {
                TripId = 1,
                UserId = 2, // Different user
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.GetReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

            _ = _mockReservationService
                .Setup(s => s.DeleteReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.DeleteReservation(1, 5, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteReservation_AsNonAdminForOtherUser_ReturnsForbid()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            Reservation reservation = new()
            {
                TripId = 1,
                UserId = 2, // Different user
                Seat = 5
            };

            _ = _mockReservationService
                .Setup(s => s.GetReservationAsync(It.Is<ReservationKeyDto>(k => k.TripId == 1 && k.Seat == 5), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservation);

            // Act
            IActionResult result = await _controller.DeleteReservation(1, 5, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ForbidResult>(result);

            _mockReservationService.Verify(
                s => s.DeleteReservationAsync(It.IsAny<ReservationKeyDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteReservation_ReservationNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            _ = _mockReservationService
                .Setup(s => s.GetReservationAsync(It.IsAny<ReservationKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Reservation not found"));

            // Act
            IActionResult result = await _controller.DeleteReservation(1, 999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}
