using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.API.Controllers;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GeneralReservationSystem.Tests.Controllers
{
    public class AuthenticationControllerTests
    {
        private readonly Mock<IAuthenticationService> _mockAuthenticationService;
        private readonly Mock<IValidator<RegisterUserDto>> _mockRegisterValidator;
        private readonly Mock<IValidator<LoginDto>> _mockLoginValidator;
        private readonly Mock<IValidator<ChangePasswordDto>> _mockChangePasswordValidator;
        private readonly AuthenticationController _controller;
        private readonly JwtSettings _jwtSettings;

        public AuthenticationControllerTests()
        {
            _mockAuthenticationService = new Mock<IAuthenticationService>();
            _mockRegisterValidator = new Mock<IValidator<RegisterUserDto>>();
            _mockLoginValidator = new Mock<IValidator<LoginDto>>();
            _mockChangePasswordValidator = new Mock<IValidator<ChangePasswordDto>>();

            _jwtSettings = new JwtSettings
            {
                SecretKey = "ThisIsATestSecretKeyForJWT_MustBeAtLeast32Characters!",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationDays = 7
            };

            _controller = new AuthenticationController(
                _mockAuthenticationService.Object,
                _jwtSettings,
                _mockRegisterValidator.Object,
                _mockLoginValidator.Object,
                _mockChangePasswordValidator.Object)
            {
                // Setup controller context with HttpContext
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Setup validators to return valid by default
            _ = _mockRegisterValidator
                .Setup(v => v.ValidateAsync(It.IsAny<RegisterUserDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockLoginValidator
                .Setup(v => v.ValidateAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ = _mockChangePasswordValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ChangePasswordDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        private void SetupAuthenticatedUser(int userId, string userName = "testuser", string email = "test@example.com", bool isAdmin = false)
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, email)
            ];

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            ClaimsIdentity identity = new(claims, "TestAuth");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext.HttpContext.User = claimsPrincipal;
        }

        #region Register Tests

        [Fact]
        public async Task Register_ValidDto_ReturnsOkWithUserId()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            UserInfo userInfo = new()
            {
                UserId = 1,
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                IsAdmin = false
            };

            _ = _mockAuthenticationService
                .Setup(s => s.RegisterUserAsync(registerDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.Register(registerDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            object? value = okResult.Value;
            Assert.NotNull(value);

            _mockAuthenticationService.Verify(
                s => s.RegisterUserAsync(registerDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Register_DuplicateUsername_ReturnsConflict()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "existinguser",
                Email = "existing@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.RegisterUserAsync(registerDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Username already exists"));

            // Act
            IActionResult result = await _controller.Register(registerDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Register_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "u", // Too short
                Email = "invalid-email",
                Password = "weak",
                ConfirmPassword = "weak"
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("UserName", "Username must be at least 3 characters"),
                new ValidationFailure("Email", "Invalid email format")
            ];

            _ = _mockRegisterValidator
                .Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.Register(registerDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockAuthenticationService.Verify(
                s => s.RegisterUserAsync(It.IsAny<RegisterUserDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Register_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.RegisterUserAsync(registerDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceException("Database error"));

            // Act
            IActionResult result = await _controller.Register(registerDto, CancellationToken.None);

            // Assert
            ObjectResult statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithUserInfo()
        {
            // Arrange
            LoginDto loginDto = new()
            {
                UserNameOrEmail = "testuser",
                Password = "SecurePassword123!"
            };

            UserInfo userInfo = new()
            {
                UserId = 1,
                UserName = "testuser",
                Email = "test@example.com",
                IsAdmin = false
            };

            _ = _mockAuthenticationService
                .Setup(s => s.AuthenticateAsync(loginDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.Login(loginDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            object? value = okResult.Value;
            Assert.NotNull(value);

            _mockAuthenticationService.Verify(
                s => s.AuthenticateAsync(loginDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsConflict()
        {
            // Arrange
            LoginDto loginDto = new()
            {
                UserNameOrEmail = "testuser",
                Password = "WrongPassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.AuthenticateAsync(loginDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Invalid credentials"));

            // Act
            IActionResult result = await _controller.Login(loginDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            LoginDto loginDto = new()
            {
                UserNameOrEmail = "", // Empty
                Password = ""
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("UserNameOrEmail", "Required"),
                new ValidationFailure("Password", "Required")
            ];

            _ = _mockLoginValidator
                .Setup(v => v.ValidateAsync(loginDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.Login(loginDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockAuthenticationService.Verify(
                s => s.AuthenticateAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Logout Tests

        [Fact]
        public void Logout_AuthenticatedUser_ReturnsOk()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            // Act
            IActionResult result = _controller.Logout();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            object? value = okResult.Value;
            Assert.NotNull(value);
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public void GetCurrentUser_AuthenticatedUser_ReturnsOkWithUserInfo()
        {
            // Arrange
            SetupAuthenticatedUser(1, "testuser", "test@example.com", isAdmin: false);

            // Act
            IActionResult result = _controller.GetCurrentUser();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo userInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(1, userInfo.UserId);
            Assert.Equal("testuser", userInfo.UserName);
            Assert.Equal("test@example.com", userInfo.Email);
            Assert.False(userInfo.IsAdmin);
        }

        [Fact]
        public void GetCurrentUser_AdminUser_ReturnsOkWithAdminFlag()
        {
            // Arrange
            SetupAuthenticatedUser(1, "admin", "admin@example.com", isAdmin: true);

            // Act
            IActionResult result = _controller.GetCurrentUser();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            UserInfo userInfo = Assert.IsType<UserInfo>(okResult.Value);
            Assert.Equal(1, userInfo.UserId);
            Assert.True(userInfo.IsAdmin);
        }

        [Fact]
        public void GetCurrentUser_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            ClaimsIdentity identity = new(); // No claims
            ClaimsPrincipal claimsPrincipal = new(identity);
            _controller.ControllerContext.HttpContext.User = claimsPrincipal;

            // Act
            IActionResult result = _controller.GetCurrentUser();

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_ValidRequest_ReturnsOk()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.ChangePasswordAsync(changePasswordDto, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            object? value = okResult.Value;
            Assert.NotNull(value);

            _mockAuthenticationService.Verify(
                s => s.ChangePasswordAsync(changePasswordDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ChangePassword_MismatchedUserId_ReturnsUnauthorized()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 2, // Different user
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            // Act
            IActionResult result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);

            _mockAuthenticationService.Verify(
                s => s.ChangePasswordAsync(It.IsAny<ChangePasswordDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ChangePassword_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            ClaimsIdentity identity = new();
            ClaimsPrincipal claimsPrincipal = new(identity);
            _controller.ControllerContext.HttpContext.User = claimsPrincipal;

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            // Act
            IActionResult result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task ChangePassword_IncorrectCurrentPassword_ReturnsConflict()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewPassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.ChangePasswordAsync(changePasswordDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Current password is incorrect"));

            // Act
            IActionResult result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(999);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 999,
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.ChangePasswordAsync(changePasswordDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceNotFoundException("User not found"));

            // Act
            IActionResult result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ValidationFails_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = "old",
                NewPassword = "new" // Too weak
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("NewPassword", "Password must be at least 8 characters")
            ];

            _ = _mockChangePasswordValidator
                .Setup(v => v.ValidateAsync(changePasswordDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act
            IActionResult result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

            // Assert
            _ = Assert.IsType<BadRequestObjectResult>(result);

            _mockAuthenticationService.Verify(
                s => s.ChangePasswordAsync(It.IsAny<ChangePasswordDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion
    }
}
