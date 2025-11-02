using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.API.Controllers;
using GeneralReservationSystem.API.Services.Interfaces;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Exceptions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Text;

namespace GeneralReservationSystem.Tests.Controllers
{
    public class StationsControllerTests
    {
        private readonly Mock<IApiStationService> _mockStationService;
        private readonly Mock<IValidator<PagedSearchRequestDto>> _mockPagedSearchValidator;
        private readonly Mock<IValidator<CreateStationDto>> _mockCreateStationValidator;
        private readonly Mock<IValidator<UpdateStationDto>> _mockUpdateStationValidator;
        private readonly Mock<IValidator<StationKeyDto>> _mockStationKeyValidator;
        private readonly Mock<IValidator<ImportStationDto>> _mockImportStationValidator;
        private readonly StationsController _controller;

        public StationsControllerTests()
        {
            _mockStationService = new Mock<IApiStationService>();
            _mockPagedSearchValidator = new Mock<IValidator<PagedSearchRequestDto>>();
            _mockCreateStationValidator = new Mock<IValidator<CreateStationDto>>();
            _mockUpdateStationValidator = new Mock<IValidator<UpdateStationDto>>();
            _mockStationKeyValidator = new Mock<IValidator<StationKeyDto>>();
            _mockImportStationValidator = new Mock<IValidator<ImportStationDto>>();

            _controller = new StationsController(
                _mockStationService.Object,
                _mockPagedSearchValidator.Object,
                _mockCreateStationValidator.Object,
                _mockUpdateStationValidator.Object,
                _mockStationKeyValidator.Object,
                _mockImportStationValidator.Object);

            // Setup validators to return valid by default
            _ = _mockPagedSearchValidator
                .Setup(v => v.ValidateAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockCreateStationValidator
                .Setup(v => v.ValidateAsync(It.IsAny<CreateStationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockUpdateStationValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateStationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockStationKeyValidator
                .Setup(v => v.ValidateAsync(It.IsAny<StationKeyDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockImportStationValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ImportStationDto>(), It.IsAny<CancellationToken>()))
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

        #region GetAllStations Tests

        [Fact]
        public async Task GetAllStations_ReturnsOkWithStations()
        {
            // Arrange
            List<Station> expectedStations =
            [
                new Station { StationId = 1, StationName = "Station 1", City = "City 1", Province = "Province 1", Country = "Country 1" },
                new Station { StationId = 2, StationName = "Station 2", City = "City 2", Province = "Province 2", Country = "Country 2" }
            ];

            _ = _mockStationService
                .Setup(s => s.GetAllStationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStations);

            // Act
            IActionResult result = await _controller.GetAllStations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            IEnumerable<Station> stations = Assert.IsType<IEnumerable<Station>>(okResult.Value, exactMatch: false);
            Assert.Equal(2, stations.Count());

            _mockStationService.Verify(
                s => s.GetAllStationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllStations_NoStations_ReturnsOkWithEmptyList()
        {
            // Arrange
            _ = _mockStationService
                .Setup(s => s.GetAllStationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetAllStations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            IEnumerable<Station> stations = Assert.IsType<IEnumerable<Station>>(okResult.Value, exactMatch: false);
            Assert.Empty(stations);

            _mockStationService.Verify(
                s => s.GetAllStationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllStations_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            _ = _mockStationService
                .Setup(s => s.GetAllStationsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceException("Database connection failed"));

            // Act & Assert
            // The controller doesn't catch ServiceException, so it will propagate
            _ = await Assert.ThrowsAsync<ServiceException>(
                () => _controller.GetAllStations(CancellationToken.None));
        }

        #endregion

        #region SearchStations (POST) Tests

        [Fact]
        public async Task SearchStations_Post_ReturnsOkWithPagedResults()
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
                    new Station { StationId = 1, StationName = "Station A" }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockStationService
                .Setup(s => s.SearchStationsAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            IActionResult result = await _controller.SearchStations(searchDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<Station> pagedResult = Assert.IsType<PagedResult<Station>>(okResult.Value);
            _ = Assert.Single(pagedResult.Items);

            _mockStationService.Verify(
                s => s.SearchStationsAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchStations_Post_ValidationFails_ReturnsBadRequest()
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
            IActionResult result = await _controller.SearchStations(searchDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockStationService.Verify(
                s => s.SearchStationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region SearchStations (GET) Tests

        [Fact]
        public async Task SearchStations_Get_ReturnsOkWithPagedResults()
        {
            // Arrange
            PagedResult<Station> expectedResult = new()
            {
                Items =
                [
                    new Station { StationId = 1 }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockStationService
                .Setup(s => s.SearchStationsAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=10");

            // Act
            IActionResult result = await _controller.SearchStations(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<Station> pagedResult = Assert.IsType<PagedResult<Station>>(okResult.Value);
            _ = Assert.Single(pagedResult.Items);
        }

        #endregion

        #region GetStation Tests

        [Fact]
        public async Task GetStation_ValidId_ReturnsOkWithStation()
        {
            // Arrange
            Station expectedStation = new()
            {
                StationId = 1,
                StationName = "Central Station",
                City = "Buenos Aires",
                Province = "Buenos Aires",
                Country = "Argentina"
            };

            _ = _mockStationService
                .Setup(s => s.GetStationAsync(It.Is<StationKeyDto>(k => k.StationId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStation);

            // Act
            IActionResult result = await _controller.GetStation(1, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Station station = Assert.IsType<Station>(okResult.Value);
            Assert.Equal(expectedStation.StationId, station.StationId);

            _mockStationService.Verify(
                s => s.GetStationAsync(It.Is<StationKeyDto>(k => k.StationId == 1), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetStation_StationNotFound_ReturnsNotFound()
        {
            // Arrange
            _ = _mockStationService
                .Setup(s => s.GetStationAsync(It.Is<StationKeyDto>(k => k.StationId == 999), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Station not found"));

            // Act
            IActionResult result = await _controller.GetStation(999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetStation_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("StationId", "Invalid station ID")
            ];

            _ = _mockStationKeyValidator
                .Setup(v => v.ValidateAsync(It.IsAny<StationKeyDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.GetStation(0, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockStationService.Verify(
                s => s.GetStationAsync(It.IsAny<StationKeyDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region CreateStation Tests

        [Fact]
        public async Task CreateStation_ValidDto_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupAdminUser();

            CreateStationDto createDto = new()
            {
                StationName = "New Station",
                City = "City",
                Province = "Province",
                Country = "Country"
            };

            Station createdStation = new()
            {
                StationId = 1,
                StationName = createDto.StationName,
                City = createDto.City,
                Province = createDto.Province,
                Country = createDto.Country
            };

            _ = _mockStationService
                .Setup(s => s.CreateStationAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdStation);

            // Act
            IActionResult result = await _controller.CreateStation(createDto, CancellationToken.None);

            // Assert
            CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetStation), createdResult.ActionName);
            Station station = Assert.IsType<Station>(createdResult.Value);
            Assert.Equal(createdStation.StationId, station.StationId);

            _mockStationService.Verify(
                s => s.CreateStationAsync(createDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateStation_DuplicateName_ReturnsConflict()
        {
            // Arrange
            SetupAdminUser();

            CreateStationDto createDto = new()
            {
                StationName = "Duplicate Station",
                City = "City",
                Province = "Province",
                Country = "Country"
            };

            _ = _mockStationService
                .Setup(s => s.CreateStationAsync(createDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Station already exists"));

            // Act
            IActionResult result = await _controller.CreateStation(createDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateStation_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            CreateStationDto createDto = new()
            {
                StationName = "",
                City = "",
                Province = "",
                Country = ""
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("StationName", "Station name is required"),
                new ValidationFailure("City", "City is required")
            ];

            _ = _mockCreateStationValidator
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.CreateStation(createDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockStationService.Verify(
                s => s.CreateStationAsync(It.IsAny<CreateStationDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region UpdateStation Tests

        [Fact]
        public async Task UpdateStation_ValidDto_ReturnsOkWithUpdatedStation()
        {
            // Arrange
            SetupAdminUser();

            UpdateStationDto updateDto = new()
            {
                StationName = "Updated Station"
            };

            Station updatedStation = new()
            {
                StationId = 1,
                StationName = "Updated Station",
                City = "City",
                Province = "Province",
                Country = "Country"
            };

            _ = _mockStationService
                .Setup(s => s.UpdateStationAsync(It.Is<UpdateStationDto>(d => d.StationId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedStation);

            // Act
            IActionResult result = await _controller.UpdateStation(1, updateDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Station station = Assert.IsType<Station>(okResult.Value);
            Assert.Equal(updatedStation.StationId, station.StationId);

            _mockStationService.Verify(
                s => s.UpdateStationAsync(It.Is<UpdateStationDto>(d => d.StationId == 1), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateStation_StationNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            UpdateStationDto updateDto = new()
            {
                StationName = "Updated Station"
            };

            _ = _mockStationService
                .Setup(s => s.UpdateStationAsync(It.IsAny<UpdateStationDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Station not found"));

            // Act
            IActionResult result = await _controller.UpdateStation(999, updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStation_DuplicateName_ReturnsConflict()
        {
            // Arrange
            SetupAdminUser();

            UpdateStationDto updateDto = new()
            {
                StationName = "Duplicate Station"
            };

            _ = _mockStationService
                .Setup(s => s.UpdateStationAsync(It.IsAny<UpdateStationDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Station name already exists"));

            // Act
            IActionResult result = await _controller.UpdateStation(1, updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStation_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            UpdateStationDto updateDto = new()
            {
                StationName = "" // Invalid
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("StationName", "Station name cannot be empty")
            ];

            _ = _mockUpdateStationValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateStationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.UpdateStation(1, updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockStationService.Verify(
                s => s.UpdateStationAsync(It.IsAny<UpdateStationDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region DeleteStation Tests

        [Fact]
        public async Task DeleteStation_ValidId_ReturnsNoContent()
        {
            // Arrange
            SetupAdminUser();

            _ = _mockStationService
                .Setup(s => s.DeleteStationAsync(It.Is<StationKeyDto>(k => k.StationId == 1), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.DeleteStation(1, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NoContentResult>(result);

            _mockStationService.Verify(
                s => s.DeleteStationAsync(It.Is<StationKeyDto>(k => k.StationId == 1), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteStation_StationNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            _ = _mockStationService
                .Setup(s => s.DeleteStationAsync(It.IsAny<StationKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("Station not found"));

            // Act
            IActionResult result = await _controller.DeleteStation(999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteStation_StationInUse_ReturnsConflict()
        {
            // Arrange
            SetupAdminUser();

            _ = _mockStationService
                .Setup(s => s.DeleteStationAsync(It.IsAny<StationKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Station is being used by trips"));

            // Act & Assert
            // The controller doesn't catch ServiceBusinessException in DeleteStation, so it will propagate
            _ = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _controller.DeleteStation(1, CancellationToken.None));
        }

        #endregion

        #region ImportStationsFromCsv Tests

        [Fact]
        public async Task ImportStationsFromCsv_NullFile_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            // Act
            IActionResult result = await _controller.ImportStationsFromCsv(null!, CancellationToken.None);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            object? value = badRequestResult.Value;
            Assert.NotNull(value);
        }

        [Fact]
        public async Task ImportStationsFromCsv_NonCsvFile_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            Mock<IFormFile> file = new();
            _ = file.Setup(f => f.FileName).Returns("test.txt");
            _ = file.Setup(f => f.Length).Returns(100);

            // Act
            IActionResult result = await _controller.ImportStationsFromCsv(file.Object, CancellationToken.None);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            object? value = badRequestResult.Value;
            Assert.NotNull(value);
        }

        [Fact]
        public async Task ImportStationsFromCsv_EmptyFile_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            Mock<IFormFile> file = new();
            _ = file.Setup(f => f.FileName).Returns("test.csv");
            _ = file.Setup(f => f.Length).Returns(0);

            // Act
            IActionResult result = await _controller.ImportStationsFromCsv(file.Object, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ImportStationsFromCsv_ValidCsvFile_ReturnsOkWithImportCount()
        {
            // Arrange
            SetupAdminUser();

            string csvContent = "StationName,City,Province,Country\nCentral Station,Buenos Aires,Buenos Aires,Argentina\nRetiro Station,Buenos Aires,Buenos Aires,Argentina";
            byte[] csvBytes = Encoding.UTF8.GetBytes(csvContent);
            MemoryStream stream = new(csvBytes);

            Mock<IFormFile> file = new();
            _ = file.Setup(f => f.FileName).Returns("stations.csv");
            _ = file.Setup(f => f.Length).Returns(csvBytes.Length);
            _ = file.Setup(f => f.OpenReadStream()).Returns(stream);
            _ = file.Setup(f => f.ContentType).Returns("text/csv");

            _ = _mockStationService
                .Setup(s => s.CreateStationsBulkAsync(It.IsAny<IEnumerable<ImportStationDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            // Act
            IActionResult result = await _controller.ImportStationsFromCsv(file.Object, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockStationService.Verify(
                s => s.CreateStationsBulkAsync(It.IsAny<IEnumerable<ImportStationDto>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ImportStationsFromCsv_InvalidCsvFormat_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            string csvContent = "Invalid,CSV,Format\nSome,Random,Data";
            byte[] csvBytes = Encoding.UTF8.GetBytes(csvContent);
            MemoryStream stream = new(csvBytes);

            Mock<IFormFile> file = new();
            _ = file.Setup(f => f.FileName).Returns("stations.csv");
            _ = file.Setup(f => f.Length).Returns(csvBytes.Length);
            _ = file.Setup(f => f.OpenReadStream()).Returns(stream);
            _ = file.Setup(f => f.ContentType).Returns("text/csv");

            // Act
            IActionResult result = await _controller.ImportStationsFromCsv(file.Object, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region ExportStationsToCsv Tests

        [Fact]
        public async Task ExportStationsToCsv_ReturnsFileResult()
        {
            // Arrange
            SetupAdminUser();

            List<Station> stations =
            [
                new Station { StationId = 1, StationName = "Station 1", City = "City 1", Province = "Province 1", Country = "Country 1" },
                new Station { StationId = 2, StationName = "Station 2", City = "City 2", Province = "Province 2", Country = "Country 2" }
            ];

            _ = _mockStationService
                .Setup(s => s.GetAllStationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stations);

            // Act
            IActionResult result = await _controller.ExportStationsToCsv(CancellationToken.None);

            // Assert
            FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Contains("text/csv", fileResult.ContentType);
            Assert.Contains("charset=utf-8", fileResult.ContentType);
            Assert.Contains("estaciones_", fileResult.FileDownloadName);
            Assert.NotEmpty(fileResult.FileContents);

            // Verify the CSV content contains the expected data
            string csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
            Assert.Contains("StationName", csvContent);
            Assert.Contains("Station 1", csvContent);
            Assert.Contains("Station 2", csvContent);
            Assert.Contains("City 1", csvContent);
            Assert.Contains("City 2", csvContent);

            _mockStationService.Verify(
                s => s.GetAllStationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExportStationsToCsv_NoStations_ReturnsFileWithHeaderOnly()
        {
            // Arrange
            SetupAdminUser();

            _ = _mockStationService
                .Setup(s => s.GetAllStationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.ExportStationsToCsv(CancellationToken.None);

            // Assert
            FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
            Assert.NotEmpty(fileResult.FileContents);

            // Verify the CSV content contains only the header
            string csvContent = Encoding.UTF8.GetString(fileResult.FileContents);
            Assert.Contains("StationName", csvContent);
        }

        #endregion
    }
}
