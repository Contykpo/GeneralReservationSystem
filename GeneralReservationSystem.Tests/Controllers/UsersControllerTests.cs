using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.API.Controllers;
using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GeneralReservationSystem.Tests.Controllers
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

        #region SearchUsers (POST) Tests

        [Fact]
        public async Task SearchUsers_Post_AsAdmin_ReturnsOkWithResults()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "user1", Email = "user1@example.com", IsAdmin = false }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserService
                .Setup(s => s.SearchUsersAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            IActionResult result = await _controller.SearchUsers(searchDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserInfo> returnedResult = Assert.IsType<PagedResult<UserInfo>>(okResult.Value);
            Assert.Equal(expectedResult.TotalCount, returnedResult.TotalCount);
            _ = Assert.Single(returnedResult.Items);

            _mockUserService.Verify(
                s => s.SearchUsersAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsers_Post_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

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
            IActionResult result = await _controller.SearchUsers(searchDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockUserService.Verify(
                s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region SearchUsers (GET) Tests

        [Fact]
        public async Task SearchUsers_Get_AsAdmin_ReturnsOkWithResults()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "user1", Email = "user1@example.com", IsAdmin = false }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserService
                .Setup(s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Setup Request.Query
            _controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=10");

            // Act
            IActionResult result = await _controller.SearchUsers(CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            PagedResult<UserInfo> returnedResult = Assert.IsType<PagedResult<UserInfo>>(okResult.Value);
            Assert.Equal(expectedResult.TotalCount, returnedResult.TotalCount);

            _mockUserService.Verify(
                s => s.SearchUsersAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()),
                Times.Once);
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
        public async Task GetCurrentUser_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            ClaimsIdentity identity = new(); // No claims
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            // Act
            IActionResult result = await _controller.GetCurrentUser(CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);

            _mockUserService.Verify(
                s => s.GetUserAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetCurrentUser_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(999);

            _ = _mockUserService
                .Setup(s => s.GetUserAsync(It.Is<UserKeyDto>(k => k.UserId == 999), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act
            IActionResult result = await _controller.GetCurrentUser(CancellationToken.None);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            object? errorResponse = notFoundResult.Value;
            Assert.NotNull(errorResponse);
        }

        #endregion

        #region GetUserById Tests

        [Fact]
        public async Task GetUserById_AsAdmin_ReturnsOkWithUser()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            User expectedUser = new()
            {
                UserId = 2,
                UserName = "user2",
                Email = "user2@example.com",
                IsAdmin = false
            };

            _ = _mockUserService
                .Setup(s => s.GetUserAsync(It.Is<UserKeyDto>(k => k.UserId == 2), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUser);

            // Act
            IActionResult result = await _controller.GetUserById(2, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            User user = Assert.IsType<User>(okResult.Value);
            Assert.Equal(expectedUser.UserId, user.UserId);
        }

        [Fact]
        public async Task GetUserById_AsSelf_ReturnsOkWithUser()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            User expectedUser = new()
            {
                UserId = 1,
                UserName = "user1",
                Email = "user1@example.com",
                IsAdmin = false
            };

            _ = _mockUserService
                .Setup(s => s.GetUserAsync(It.Is<UserKeyDto>(k => k.UserId == 1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUser);

            // Act
            IActionResult result = await _controller.GetUserById(1, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            User user = Assert.IsType<User>(okResult.Value);
            Assert.Equal(expectedUser.UserId, user.UserId);
        }

        [Fact]
        public async Task GetUserById_AsNonAdminForOtherUser_ReturnsForbid()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            // Act
            IActionResult result = await _controller.GetUserById(2, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ForbidResult>(result);

            _mockUserService.Verify(
                s => s.GetUserAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetUserById_NoUserIdClaim_ReturnsUnauthorized()
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
            IActionResult result = await _controller.GetUserById(1, CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetUserById_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            _ = _mockUserService
                .Setup(s => s.GetUserAsync(It.Is<UserKeyDto>(k => k.UserId == 999), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act
            IActionResult result = await _controller.GetUserById(999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
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
        public async Task UpdateCurrentUser_NoUserIdClaim_ReturnsUnauthorized()
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

            UpdateUserDto updateDto = new()
            {
                UserName = "updateduser"
            };

            // Act
            IActionResult result = await _controller.UpdateCurrentUser(updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);

            _mockUserService.Verify(
                s => s.UpdateUserAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateCurrentUser_UserNotFound_ReturnsNotFound()
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

            // Act
            IActionResult result = await _controller.UpdateCurrentUser(updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCurrentUser_DuplicateUserName_ReturnsConflict()
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

            // Act
            IActionResult result = await _controller.UpdateCurrentUser(updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCurrentUser_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            UpdateUserDto updateDto = new()
            {
                UserName = "" // Invalid
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("UserName", "UserName cannot be empty")
            ];

            _ = _mockUpdateUserValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.UpdateCurrentUser(updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

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
        public async Task UpdateUserById_AsNonAdminForOtherUser_ReturnsForbid()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            UpdateUserDto updateDto = new()
            {
                UserName = "updateduser"
            };

            // Act
            IActionResult result = await _controller.UpdateUserById(2, updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ForbidResult>(result);

            _mockUserService.Verify(
                s => s.UpdateUserAsync(It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateUserById_DuplicateUserName_ReturnsConflict()
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

            // Act
            IActionResult result = await _controller.UpdateUserById(2, updateDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
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
        public async Task DeleteCurrentUser_NoUserIdClaim_ReturnsUnauthorized()
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
            IActionResult result = await _controller.DeleteCurrentUser(CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);

            _mockUserService.Verify(
                s => s.DeleteUserAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteCurrentUser_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(999);

            _ = _mockUserService
                .Setup(s => s.DeleteUserAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act
            IActionResult result = await _controller.DeleteCurrentUser(CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
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
        public async Task DeleteUserById_AsNonAdminForOtherUser_ReturnsForbid()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: false);

            // Act
            IActionResult result = await _controller.DeleteUserById(2, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ForbidResult>(result);

            _mockUserService.Verify(
                s => s.DeleteUserAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteUserById_NoUserIdClaim_ReturnsUnauthorized()
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
            IActionResult result = await _controller.DeleteUserById(1, CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeleteUserById_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(1, isAdmin: true);

            _ = _mockUserService
                .Setup(s => s.DeleteUserAsync(It.IsAny<UserKeyDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act
            IActionResult result = await _controller.DeleteUserById(999, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}
