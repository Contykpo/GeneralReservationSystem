using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Services.Interfaces.Authentication;
using GeneralReservationSystem.Infrastructure.Helpers;
using GeneralReservationSystem.Server.Controllers.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GeneralReservationSystem.Tests.Controllers.Authentication
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

        #region Register User Tests

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
        public async Task Register_DuplicateUsername_ThrowsServiceBusinessException()
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

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _controller.Register(registerDto, CancellationToken.None));
        }

        [Fact]
        public async Task Register_ValidationFails_ThrowsServiceValidationException()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "u",
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

            // Act & Assert
            ServiceValidationException exception = await Assert.ThrowsAsync<ServiceValidationException>(
                () => _controller.Register(registerDto, CancellationToken.None));

            Assert.Equal(2, exception.Errors.Length);
            Assert.Contains(exception.Errors, e => e.Field == "UserName");
            Assert.Contains(exception.Errors, e => e.Field == "Email");

            _mockAuthenticationService.Verify(
                s => s.RegisterUserAsync(It.IsAny<RegisterUserDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Register_ServiceError_ThrowsServiceException()
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

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceException>(
                () => _controller.Register(registerDto, CancellationToken.None));
        }

        #endregion

        #region Register Admin Tests

        [Fact]
        public async Task RegisterAdmin_ValidDto_ReturnsOkWithUserId()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "adminuser",
                Email = "adminuser@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            UserInfo userInfo = new()
            {
                UserId = 42,
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                IsAdmin = true
            };

            _ = _mockAuthenticationService
                .Setup(s => s.RegisterAdminAsync(registerDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userInfo);

            // Act
            IActionResult result = await _controller.RegisterAdmin(registerDto, CancellationToken.None);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            dynamic? response = okResult.Value;
            Assert.NotNull(response);
            Assert.Contains("Administrador registrado exitosamente", response?.message.ToString());
            Assert.Equal(42, (int?)response?.userId);

            _mockAuthenticationService.Verify(
                s => s.RegisterAdminAsync(registerDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAdmin_DuplicateUsernameOrEmail_ThrowsServiceBusinessException()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "duplicateadmin",
                Email = "dup@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.RegisterAdminAsync(registerDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Ya existe un usuario con el mismo nombre o correo electrónico."));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _controller.RegisterAdmin(registerDto, CancellationToken.None));

            Assert.Contains("Ya existe un usuario", exception.Message);

            _mockAuthenticationService.Verify(
                s => s.RegisterAdminAsync(registerDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAdmin_ValidationFails_ThrowsServiceValidationException()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "a",
                Email = "invalid-email",
                Password = "123",
                ConfirmPassword = "456"
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("Email", "Formato de email inválido"),
                new ValidationFailure("Password", "Las contraseñas no coinciden")
            ];

            _ = _mockRegisterValidator
                .Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act & Assert
            ServiceValidationException exception = await Assert.ThrowsAsync<ServiceValidationException>(
                () => _controller.RegisterAdmin(registerDto, CancellationToken.None));

            Assert.Equal(2, exception.Errors.Length);
            Assert.Contains(exception.Errors, e => e.Field == "Email");
            Assert.Contains(exception.Errors, e => e.Field == "Password");

            _mockAuthenticationService.Verify(
                s => s.RegisterAdminAsync(It.IsAny<RegisterUserDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAdmin_ServiceError_ThrowsServiceException()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "erroradmin",
                Email = "error@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.RegisterAdminAsync(registerDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceException("Error al registrar el administrador."));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _controller.RegisterAdmin(registerDto, CancellationToken.None));

            Assert.Contains("Error al registrar el administrador", exception.Message);

            _mockAuthenticationService.Verify(
                s => s.RegisterAdminAsync(registerDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAdmin_ThrowsServiceBusinessException_WhenServiceBusinessExceptionThrown()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "conflictadmin",
                Email = "conflict@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            _ = _mockAuthenticationService
                .Setup(s => s.RegisterAdminAsync(registerDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServiceBusinessException("Administrador duplicado"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _controller.RegisterAdmin(registerDto, CancellationToken.None));
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
        public async Task Login_InvalidCredentials_ThrowsServiceBusinessException()
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

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _controller.Login(loginDto, CancellationToken.None));
        }

        [Fact]
        public async Task Login_ValidationFails_ThrowsServiceValidationException()
        {
            // Arrange
            LoginDto loginDto = new()
            {
                UserNameOrEmail = "",
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

            // Act & Assert
            ServiceValidationException exception = await Assert.ThrowsAsync<ServiceValidationException>(
                () => _controller.Login(loginDto, CancellationToken.None));

            Assert.Equal(2, exception.Errors.Length);

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
        public async Task ChangePassword_IncorrectCurrentPassword_ThrowsServiceBusinessException()
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

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _controller.ChangePassword(changePasswordDto, CancellationToken.None));
        }

        [Fact]
        public async Task ChangePassword_UserNotFound_ThrowsServiceNotFoundException()
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

            // Act & Assert
            _ = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _controller.ChangePassword(changePasswordDto, CancellationToken.None));
        }

        [Fact]
        public async Task ChangePassword_ValidationFails_ThrowsServiceValidationException()
        {
            // Arrange
            SetupAuthenticatedUser(1);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = "old",
                NewPassword = "new"
            };

            List<ValidationFailure> validationFailures =
            [
                new ValidationFailure("NewPassword", "Password must be at least 8 characters")
            ];

            _ = _mockChangePasswordValidator
                .Setup(v => v.ValidateAsync(changePasswordDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act & Assert
            ServiceValidationException exception = await Assert.ThrowsAsync<ServiceValidationException>(
                () => _controller.ChangePassword(changePasswordDto, CancellationToken.None));

            _ = Assert.Single(exception.Errors);
            Assert.Equal("NewPassword", exception.Errors[0].Field);

            _mockAuthenticationService.Verify(
                s => s.ChangePasswordAsync(It.IsAny<ChangePasswordDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion
    }
}
