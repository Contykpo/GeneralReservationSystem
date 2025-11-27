using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Server.Controllers.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Security.Claims;

namespace GeneralReservationSystem.Tests.Controllers.Authentication
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IValidator<PagedSearchRequestDto>> _mockPagedSearchValidator;
        private readonly Mock<IValidator<UpdateUserDto>> _mockUpdateUserValidator;
        private readonly Mock<IValidator<UserKeyDto>> _mockUserKeyValidator;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockPagedSearchValidator = new Mock<IValidator<PagedSearchRequestDto>>();
            _mockUpdateUserValidator = new Mock<IValidator<UpdateUserDto>>();
            _mockUserKeyValidator = new Mock<IValidator<UserKeyDto>>();

            _controller = new UsersController(
                _mockUserService.Object,
                _mockPagedSearchValidator.Object,
                _mockUpdateUserValidator.Object,
                _mockUserKeyValidator.Object);

            // Setup validators to return valid by default
            _ = _mockPagedSearchValidator
                .Setup(v => v.ValidateAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockUpdateUserValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
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

        #region SearchUsers Tests

        [Fact]
        public async Task SearchUsers_NoFiltersOrOrders_ReturnsPagedResult()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<UserInfo> users =
            [
                new UserInfo
                {
                    UserId = 1,
                    UserName = "user1",
                    Email = "user1@example.com",
                    IsAdmin = false
                },
                new UserInfo
                {
                    UserId = 2,
                    UserName = "user2",
                    Email = "user2@example.com",
                    IsAdmin = false
                }
            ];

            PagedResult<UserInfo> expectedResult = new()
            {
                Items = users,
                TotalCount = 2,
                Page = 1,
                PageSize = 20
            };

            _ = _mockUserService
                .Setup(s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
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
            IActionResult result = await _controller.SearchUsers(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserInfo> pagedResult = Assert.IsType<PagedResult<UserInfo>>(okResult.Value);
            Assert.Equal(2, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.Items.Count());

            _mockUserService.Verify(
                s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsers_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<UserInfo> users =
            [
                new UserInfo
                {
                    UserId = 3,
                    UserName = "user3",
                    Email = "user3@example.com",
                    IsAdmin = false
                }
            ];

            PagedResult<UserInfo> expectedResult = new()
            {
                Items = users,
                TotalCount = 10,
                Page = 2,
                PageSize = 5
            };

            _ = _mockUserService
                .Setup(s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
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
            IActionResult result = await _controller.SearchUsers(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserInfo> pagedResult = Assert.IsType<PagedResult<UserInfo>>(okResult.Value);
            Assert.Equal(2, pagedResult.Page);
            Assert.Equal(5, pagedResult.PageSize);
            Assert.Equal(10, pagedResult.TotalCount);
        }

        [Fact]
        public async Task SearchUsers_WithFilters_ReturnsFilteredResults()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<UserInfo> users =
            [
                new UserInfo
                {
                    UserId = 1,
                    UserName = "adminuser",
                    Email = "admin@example.com",
                    IsAdmin = true
                }
            ];

            PagedResult<UserInfo> expectedResult = new()
            {
                Items = users,
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            };

            _ = _mockUserService
                .Setup(s => s.SearchUsersAsync(It.Is<PagedSearchRequestDto>(dto =>
                    dto.FilterClauses.Any()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "filters", new StringValues("[IsAdmin|Equals|true]") }
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
            IActionResult result = await _controller.SearchUsers(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserInfo> pagedResult = Assert.IsType<PagedResult<UserInfo>>(okResult.Value);
            _ = Assert.Single(pagedResult.Items);
        }

        [Fact]
        public async Task SearchUsers_WithOrders_ReturnsSortedResults()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            List<UserInfo> users =
            [
                new UserInfo
                {
                    UserId = 2,
                    UserName = "userb",
                    Email = "userb@example.com",
                    IsAdmin = false
                },
                new UserInfo
                {
                    UserId = 1,
                    UserName = "usera",
                    Email = "usera@example.com",
                    IsAdmin = false
                }
            ];

            PagedResult<UserInfo> expectedResult = new()
            {
                Items = users,
                TotalCount = 2,
                Page = 1,
                PageSize = 20
            };

            _ = _mockUserService
                .Setup(s => s.SearchUsersAsync(It.Is<PagedSearchRequestDto>(dto =>
                    dto.Orders.Any()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            QueryCollection queryCollection = new(new Dictionary<string, StringValues>
            {
                { "orders", new StringValues("UserName|Desc") }
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
            IActionResult result = await _controller.SearchUsers(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserInfo> pagedResult = Assert.IsType<PagedResult<UserInfo>>(okResult.Value);
            Assert.Equal(2, pagedResult.Items.Count());
        }

        [Fact]
        public async Task SearchUsers_ValidationFails_ThrowsServiceValidationException()
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

            // Act & Assert
            ServiceValidationException exception = await Assert.ThrowsAsync<ServiceValidationException>(
                () => _controller.SearchUsers(CancellationToken.None));

            _ = Assert.Single(exception.Errors);

            _mockUserService.Verify(
                s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SearchUsers_EmptyResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            PagedResult<UserInfo> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            };

            _ = _mockUserService
                .Setup(s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
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
            IActionResult result = await _controller.SearchUsers(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserInfo> pagedResult = Assert.IsType<PagedResult<UserInfo>>(okResult.Value);
            Assert.Empty(pagedResult.Items);
            Assert.Equal(0, pagedResult.TotalCount);
        }

        [Fact]
        public async Task SearchUsers_ServiceError_ThrowsServiceException()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            _ = _mockUserService
                .Setup(s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
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
                () => _controller.SearchUsers(CancellationToken.None));
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public async Task GetCurrentUser_ValidUser_ReturnsOkWithUserInfo()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            User expectedUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                Email = "test@example.com",
                IsAdmin = false
            };

            _ = _mockUserService
                .Setup(s => s.GetUserAsync(It.Is<UserKeyDto>(k => k.UserId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUser);

            // Act
            IActionResult result = await _controller.GetCurrentUser(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo userInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(expectedUser.UserId, userInfo.UserId);
            Assert.Equal(expectedUser.UserName, userInfo.UserName);
            Assert.Equal(expectedUser.Email, userInfo.Email);
            Assert.Equal(expectedUser.IsAdmin, userInfo.IsAdmin);
        }

        [Fact]
        public async Task GetCurrentUser_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            SetupAuthenticatedUser(999);

            _ = _mockUserService
                .Setup(s => s.GetUserAsync(It.Is<UserKeyDto>(k => k.UserId == 999), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _controller.GetCurrentUser(CancellationToken.None));
        }

        #endregion

        #region GetUserById Tests

        [Fact]
        public async Task GetUserById_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            _ = _mockUserService
                .Setup(s => s.GetUserAsync(It.Is<UserKeyDto>(k => k.UserId == 999), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _controller.GetUserById(999, CancellationToken.None));
        }

        #endregion

        #region UpdateCurrentUser Tests

        [Fact]
        public async Task UpdateCurrentUser_ValidUpdate_ReturnsOkWithUserInfo()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            UpdateUserDto updateDto = new()
            {
                UserName = "updateduser",
                Email = "updated@example.com"
            };

            User updatedUser = new()
            {
                UserId = 1,
                UserName = "updateduser",
                Email = "updated@example.com",
                IsAdmin = false
            };

            _ = _mockUserService
                .Setup(s => s.UpdateUserAsync(It.Is<UpdateUserDto>(d => d.UserId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedUser);

            // Act
            IActionResult result = await _controller.UpdateCurrentUser(updateDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo userInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(updatedUser.UserId, userInfo.UserId);
            Assert.Equal(updatedUser.UserName, userInfo.UserName);
            Assert.Equal(updatedUser.Email, userInfo.Email);
        }

        [Fact]
        public async Task UpdateCurrentUser_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            SetupAuthenticatedUser(999);

            UpdateUserDto updateDto = new()
            {
                UserName = "updateduser"
            };

            _ = _mockUserService
                .Setup(s => s.UpdateUserAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _controller.UpdateCurrentUser(updateDto, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateCurrentUser_DuplicateUserName_ThrowsServiceBusinessException()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            UpdateUserDto updateDto = new()
            {
                UserName = "duplicateuser"
            };

            _ = _mockUserService
                .Setup(s => s.UpdateUserAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Username already exists"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _controller.UpdateCurrentUser(updateDto, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateCurrentUser_ValidationFails_ThrowsServiceValidationException()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            UpdateUserDto updateDto = new()
            {
                UserName = ""
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("UserName", "UserName cannot be empty")
            ];

            _ = _mockUpdateUserValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act & Assert
            ServiceValidationException exception = await Assert.ThrowsAsync<ServiceValidationException>(
                () => _controller.UpdateCurrentUser(updateDto, CancellationToken.None));

            _ = Assert.Single(exception.Errors);

            _mockUserService.Verify(
                s => s.UpdateUserAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region UpdateUserById Tests

        [Fact]
        public async Task UpdateUserById_AsAdmin_ReturnsOkWithUserInfo()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            UpdateUserDto updateDto = new()
            {
                UserName = "updateduser",
                Email = "updated@example.com"
            };

            User updatedUser = new()
            {
                UserId = 2,
                UserName = "updateduser",
                Email = "updated@example.com",
                IsAdmin = false
            };

            _ = _mockUserService
                .Setup(s => s.UpdateUserAsync(It.Is<UpdateUserDto>(d => d.UserId == 2), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedUser);

            // Act
            IActionResult result = await _controller.UpdateUserById(2, updateDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo userInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(updatedUser.UserId, userInfo.UserId);
        }

        [Fact]
        public async Task UpdateUserById_AsSelf_ReturnsOkWithUserInfo()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            UpdateUserDto updateDto = new()
            {
                UserName = "updateduser"
            };

            User updatedUser = new()
            {
                UserId = 1,
                UserName = "updateduser",
                Email = "user1@example.com",
                IsAdmin = false
            };

            _ = _mockUserService
                .Setup(s => s.UpdateUserAsync(It.Is<UpdateUserDto>(d => d.UserId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedUser);

            // Act
            IActionResult result = await _controller.UpdateUserById(1, updateDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo userInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(updatedUser.UserId, userInfo.UserId);
        }

        [Fact]
        public async Task UpdateUserById_DuplicateUserName_ThrowsServiceBusinessException()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            UpdateUserDto updateDto = new()
            {
                UserName = "duplicateuser"
            };

            _ = _mockUserService
                .Setup(s => s.UpdateUserAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Username already exists"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _controller.UpdateUserById(2, updateDto, CancellationToken.None));
        }

        #endregion

        #region DeleteCurrentUser Tests

        [Fact]
        public async Task DeleteCurrentUser_ValidUser_ReturnsNoContent()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            _ = _mockUserService
                .Setup(s => s.DeleteUserAsync(It.Is<UserKeyDto>(k => k.UserId == 1), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.DeleteCurrentUser(CancellationToken.None);

            // Assert
            _ = Assert.IsType<NoContentResult>(result);

            _mockUserService.Verify(
                s => s.DeleteUserAsync(It.Is<UserKeyDto>(k => k.UserId == 1), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteCurrentUser_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            SetupAuthenticatedUser(999);

            _ = _mockUserService
                .Setup(s => s.DeleteUserAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _controller.DeleteCurrentUser(CancellationToken.None));
        }

        #endregion

        #region DeleteUserById Tests

        [Fact]
        public async Task DeleteUserById_AsAdmin_ReturnsNoContent()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            _ = _mockUserService
                .Setup(s => s.DeleteUserAsync(It.Is<UserKeyDto>(k => k.UserId == 2), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.DeleteUserById(2, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NoContentResult>(result);

            _mockUserService.Verify(
                s => s.DeleteUserAsync(It.Is<UserKeyDto>(k => k.UserId == 2), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteUserById_AsSelf_ReturnsNoContent()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            _ = _mockUserService
                .Setup(s => s.DeleteUserAsync(It.Is<UserKeyDto>(k => k.UserId == 1), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.DeleteUserById(1, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUserById_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            _ = _mockUserService
                .Setup(s => s.DeleteUserAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _controller.DeleteUserById(999, CancellationToken.None));
        }

        #endregion
    }
}
