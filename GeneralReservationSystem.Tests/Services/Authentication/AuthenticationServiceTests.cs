using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Helpers;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication;
using Moq;

namespace GeneralReservationSystem.Tests.Services.Authentication
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly AuthenticationService _authenticationService;

        public AuthenticationServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _authenticationService = new AuthenticationService(_mockUserRepository.Object);
        }

        #region RegisterUserAsync Tests

        [Fact]
        public async Task RegisterUserAsync_ValidDto_ReturnsUserInfo()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .Callback<User, CancellationToken>((user, ct) =>
                {
                    user.UserId = 1; // Simulate database setting the ID
                });

            // Act
            UserInfo result = await _authenticationService.RegisterUserAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal(registerDto.UserName, result.UserName);
            Assert.Equal(registerDto.Email, result.Email);
            Assert.False(result.IsAdmin);

            _mockUserRepository.Verify(
                repo => repo.CreateAsync(
                    It.Is<User>(u =>
                        u.UserName == registerDto.UserName &&
                        u.Email == registerDto.Email &&
                        u.IsAdmin == false &&
                        u.PasswordHash != null &&
                        u.PasswordSalt != null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterUserAsync_DuplicateUserNameOrEmail_ThrowsServiceBusinessException()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "existinguser",
                Email = "existing@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UniqueConstraintViolationException("UQ_User_UserName"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _authenticationService.RegisterUserAsync(registerDto));

            Assert.Equal("Ya existe un usuario con el mismo nombre o correo electrónico.", exception.Message);
            _ = Assert.IsType<UniqueConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task RegisterUserAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _authenticationService.RegisterUserAsync(registerDto));

            Assert.Equal("Error al registrar el usuario.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task RegisterUserAsync_PasswordIsHashed_CreatesUserWithHashedPassword()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };

            User? capturedUser = null;
            _ = _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .Callback<User, CancellationToken>((user, ct) =>
                {
                    capturedUser = user;
                    user.UserId = 1;
                });

            // Act
            _ = await _authenticationService.RegisterUserAsync(registerDto);

            // Assert
            Assert.NotNull(capturedUser);
            Assert.NotNull(capturedUser.PasswordHash);
            Assert.NotNull(capturedUser.PasswordSalt);
            Assert.NotEmpty(capturedUser.PasswordHash);
            Assert.NotEmpty(capturedUser.PasswordSalt);
            // Verify the password can be verified with the hash
            Assert.True(PasswordHelper.VerifyPassword(registerDto.Password, capturedUser.PasswordHash, capturedUser.PasswordSalt));
        }

        [Fact]
        public async Task RegisterUserAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            RegisterUserDto registerDto = new()
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "SecurePassword123!",
                ConfirmPassword = "SecurePassword123!"
            };
            CancellationToken cancellationToken = new();

            _ = _mockUserRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<User>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            _ = await _authenticationService.RegisterUserAsync(registerDto, cancellationToken);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<User>(), cancellationToken),
                Times.Once);
        }

		#endregion

		#region RegisterAdminAsync Tests

        public readonly RegisterUserDto registerAdminDTO = new()
		{
			UserName        = "adminuser",
			Password        = "gigacontraseña",
			ConfirmPassword = "gigacontraseña",
			Email           = "admin@grs.com"
		};

		[Fact]
		public async Task RegisterAdminAsync_ShouldReturn_UserInfo_WithAdminFlagTrue()
		{
			_mockUserRepository
				.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(1);

			// Act
			UserInfo result = await _authenticationService.RegisterAdminAsync(registerAdminDTO);

			// Assert (resultado)
			Assert.NotNull  (result                                 );
			Assert.Equal    (registerAdminDTO.UserName,  result.UserName );
			Assert.Equal    (registerAdminDTO.Email,     result.Email    );
			Assert.True     (result.IsAdmin                         );

			// Assert (interacción)
			_mockUserRepository.Verify(
				repo => repo.CreateAsync(
					It.Is<User>(u =>
						u.UserName      == registerAdminDTO.UserName &&
						u.Email         == registerAdminDTO.Email    &&
						u.IsAdmin       == true                 &&
						u.PasswordHash  != null                 &&
						u.PasswordSalt  != null),
					It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[Fact]
		public async Task RegisterAdminAsync_WhenDuplicateUser_ThrowsServiceBusinessException()
		{
			_mockUserRepository
				.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new UniqueConstraintViolationException("UQ_User_UserName"));

			// Act & Assert
			ServiceBusinessException ex = await Assert.ThrowsAsync<ServiceBusinessException>(
				() => _authenticationService.RegisterAdminAsync(registerAdminDTO));

			Assert.Equal("Ya existe un usuario con el mismo nombre o correo electrónico.", ex.Message);
			Assert.IsType<UniqueConstraintViolationException>(ex.InnerException);

			_mockUserRepository.Verify(
				r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[Fact]
		public async Task RegisterAdminAsync_WhenRepositoryFails_ThrowsServiceException()
		{
			_mockUserRepository
				.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new RepositoryException("DB error"));

			// Act & Assert
			ServiceException ex = await Assert.ThrowsAsync<ServiceException>(
				() => _authenticationService.RegisterAdminAsync(registerAdminDTO));

			Assert.Equal("Error al registrar el usuario.", ex.Message);
			Assert.IsType<RepositoryException>(ex.InnerException);

			_mockUserRepository.Verify(
				r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[Fact]
		public async Task RegisterAdminAsync_ShouldNotPassNormalizedFields()
		{
			_mockUserRepository
				.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(1);

			// Act
			await _authenticationService.RegisterAdminAsync(registerAdminDTO);

			// Assert
			_mockUserRepository.Verify(
				repo => repo.CreateAsync(
					It.Is<User>(u =>
						u.UserName                  == registerAdminDTO.UserName &&
						u.Email                     == registerAdminDTO.Email    &&
						u.IsAdmin                   == true                 &&
						u.PasswordHash              != null                 &&
						u.PasswordSalt              != null                 &&
						u.NormalizedUserName        == null                 && // Propiedades [Computed] no se setean aquí
						u.NormalizedEmail           == null                 ),
					It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[Fact]
		public async Task RegisterAdminAsync_SamePassword_ShouldGenerateDifferentHashAndSalt()
		{
			// Arrange
			RegisterUserDto dto1 = new()
			{
				UserName        = "admin1",
				Password        = "123456",
				ConfirmPassword = "123456",
				Email           = "admin1@grs.com"
			};

			RegisterUserDto dto2 = new()
			{
				UserName            = "admin2",
				Password            = "123456", // misma contraseña a propósito
				ConfirmPassword     = "123456",
				Email               = "admin2@grs.com"
			};

			User? capturedUser1 = null;
			User? capturedUser2 = null;
            int ctr = 0;

            _mockUserRepository
                .Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .Callback<User, CancellationToken>((u, _) =>
                {
                    if (ctr == 0)
                        capturedUser1 = u;
                    else
                        capturedUser2 = u;

                    ctr++;
                });

			// Act
			await _authenticationService.RegisterAdminAsync(dto1);
			await _authenticationService.RegisterAdminAsync(dto2);

			Assert.NotNull(capturedUser1);
			Assert.NotNull(capturedUser2);

			//Misma contraseña genera distintos hashes y salts
			Assert.False(capturedUser1!.PasswordHash.SequenceEqual(capturedUser2!.PasswordHash));
			Assert.False(capturedUser1.PasswordSalt.SequenceEqual(capturedUser2.PasswordSalt));

			Assert.NotEmpty(capturedUser1.PasswordHash);
			Assert.NotEmpty(capturedUser1.PasswordSalt);
			Assert.NotEmpty(capturedUser2.PasswordHash);
			Assert.NotEmpty(capturedUser2.PasswordSalt);

			_mockUserRepository.Verify(
				repo => repo.CreateAsync(
					It.Is<User>(u => 
                        u.IsAdmin                   && 
                        u.PasswordHash.Length > 0   && 
                        u.PasswordSalt.Length > 0),
					It.IsAny<CancellationToken>()),
				Times.Exactly(2));
		}

		[Fact]
		public async Task RegisterAdminAsync_ShouldGenerateVerifiablePasswordHash()
		{
			User? capturedUser = null;

			_mockUserRepository
				.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(1)
				.Callback<User, CancellationToken>((u, _) => capturedUser = u);

			// Act
			await _authenticationService.RegisterAdminAsync(registerAdminDTO);

			// Assert
			Assert.NotNull(capturedUser);
			Assert.NotEmpty(capturedUser!.PasswordHash);
			Assert.NotEmpty(capturedUser.PasswordSalt);

			//La contraseña debe poder verificarse correctamente
			bool verified = PasswordHelper.VerifyPassword(registerAdminDTO.Password, capturedUser.PasswordHash, capturedUser.PasswordSalt);
			Assert.True(verified, "La contraseña debería verificarse correctamente con el hash y salt generados.");

			_mockUserRepository.Verify(
				r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[Fact]
		public async Task RegisterAdminAsync_WithCancellationToken_PassesTokenToRepository()
		{
			_mockUserRepository
				.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(1);

            CancellationToken cancellationToken = new();

			// Act
			await _authenticationService.RegisterAdminAsync(registerAdminDTO, cancellationToken);

			_mockUserRepository.Verify(
				r => r.CreateAsync(It.IsAny<User>(), cancellationToken),
				Times.Once,
                $"{nameof(_authenticationService.RegisterAdminAsync)} no paso {nameof(cancellationToken)} al repositorio");
		}

		#endregion

		#region AuthenticateAsync Tests

		[Fact]
        public async Task AuthenticateAsync_ValidCredentials_ReturnsUserInfo()
        {
            // Arrange
            string password = "SecurePassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(password);

            LoginDto loginDto = new()
            {
                UserNameOrEmail = "testuser",
                Password = password
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                Email = "testuser@example.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                IsAdmin = false
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByUserNameOrEmailAsync("TESTUSER", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            UserInfo result = await _authenticationService.AuthenticateAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingUser.UserId, result.UserId);
            Assert.Equal(existingUser.UserName, result.UserName);
            Assert.Equal(existingUser.Email, result.Email);
            Assert.Equal(existingUser.IsAdmin, result.IsAdmin);

            _mockUserRepository.Verify(
                repo => repo.GetByUserNameOrEmailAsync("TESTUSER", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_ValidCredentialsWithEmail_ReturnsUserInfo()
        {
            // Arrange
            string password = "SecurePassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(password);

            LoginDto loginDto = new()
            {
                UserNameOrEmail = "testuser@example.com",
                Password = password
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                Email = "testuser@example.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                IsAdmin = false
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByUserNameOrEmailAsync("TESTUSER@EXAMPLE.COM", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            UserInfo result = await _authenticationService.AuthenticateAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingUser.UserId, result.UserId);
        }

        [Fact]
        public async Task AuthenticateAsync_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            LoginDto loginDto = new()
            {
                UserNameOrEmail = "nonexistentuser",
                Password = "SomePassword123!"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByUserNameOrEmailAsync("NONEXISTENTUSER", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _authenticationService.AuthenticateAsync(loginDto));

            Assert.Equal("No se encontró el usuario.", exception.Message);
        }

        [Fact]
        public async Task AuthenticateAsync_InvalidPassword_ThrowsServiceBusinessException()
        {
            // Arrange
            string correctPassword = "CorrectPassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(correctPassword);

            LoginDto loginDto = new()
            {
                UserNameOrEmail = "testuser",
                Password = "WrongPassword123!"
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                Email = "testuser@example.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                IsAdmin = false
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByUserNameOrEmailAsync("TESTUSER", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _authenticationService.AuthenticateAsync(loginDto));

            Assert.Equal("Usuario o contraseña incorrectos.", exception.Message);
        }

        [Fact]
        public async Task AuthenticateAsync_NormalizesInput_CallsRepositoryWithUpperInvariant()
        {
            // Arrange
            string password = "SecurePassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(password);

            LoginDto loginDto = new()
            {
                UserNameOrEmail = "  TestUser  ", // With spaces and mixed case
                Password = password
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "TestUser",
                Email = "testuser@example.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                IsAdmin = false
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByUserNameOrEmailAsync("TESTUSER", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            _ = await _authenticationService.AuthenticateAsync(loginDto);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.GetByUserNameOrEmailAsync("TESTUSER", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            LoginDto loginDto = new()
            {
                UserNameOrEmail = "testuser",
                Password = "SomePassword123!"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByUserNameOrEmailAsync("TESTUSER", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _authenticationService.AuthenticateAsync(loginDto));

            Assert.Equal("Error al autenticar el usuario.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task AuthenticateAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            string password = "SecurePassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(password);

            LoginDto loginDto = new()
            {
                UserNameOrEmail = "testuser",
                Password = password
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                PasswordHash = hash,
                PasswordSalt = salt
            };

            CancellationToken cancellationToken = new();

            _ = _mockUserRepository
                .Setup(repo => repo.GetByUserNameOrEmailAsync("TESTUSER", cancellationToken))
                .ReturnsAsync(existingUser);

            // Act
            _ = await _authenticationService.AuthenticateAsync(loginDto, cancellationToken);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.GetByUserNameOrEmailAsync("TESTUSER", cancellationToken),
                Times.Once);
        }

        #endregion

        #region ChangePasswordAsync Tests

        [Fact]
        public async Task ChangePasswordAsync_ValidCurrentPassword_ChangesPassword()
        {
            // Arrange
            string currentPassword = "OldPassword123!";
            string newPassword = "NewPassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(currentPassword);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                Email = "testuser@example.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                IsAdmin = false
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _authenticationService.ChangePasswordAsync(changePasswordDto);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.UpdateAsync(
                    It.Is<User>(u =>
                        u.UserId == 1 &&
                        u.PasswordHash != null &&
                        u.PasswordSalt != null &&
                        PasswordHelper.VerifyPassword(newPassword, u.PasswordHash, u.PasswordSalt)),
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 999,
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _authenticationService.ChangePasswordAsync(changePasswordDto));

            Assert.Equal("No se encontró el usuario para cambiar la contraseña.", exception.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_IncorrectCurrentPassword_ThrowsServiceBusinessException()
        {
            // Arrange
            string currentPassword = "OldPassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(currentPassword);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewPassword123!"
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                PasswordHash = hash,
                PasswordSalt = salt
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _authenticationService.ChangePasswordAsync(changePasswordDto));

            Assert.Equal("La contraseña actual es incorrecta.", exception.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_UpdateFails_ThrowsServiceNotFoundException()
        {
            // Arrange
            string currentPassword = "OldPassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(currentPassword);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = currentPassword,
                NewPassword = "NewPassword123!"
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                PasswordHash = hash,
                PasswordSalt = salt
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _authenticationService.ChangePasswordAsync(changePasswordDto));

            Assert.Equal("No se pudo cambiar la contraseña.", exception.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            string currentPassword = "OldPassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(currentPassword);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = currentPassword,
                NewPassword = "NewPassword123!"
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                PasswordHash = hash,
                PasswordSalt = salt
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _authenticationService.ChangePasswordAsync(changePasswordDto));

            Assert.Equal("Error al cambiar la contraseña.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task ChangePasswordAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            string currentPassword = "OldPassword123!";
            (byte[] hash, byte[] salt) = PasswordHelper.HashPassword(currentPassword);

            ChangePasswordDto changePasswordDto = new()
            {
                UserId = 1,
                CurrentPassword = currentPassword,
                NewPassword = "NewPassword123!"
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                PasswordHash = hash,
                PasswordSalt = salt
            };

            CancellationToken cancellationToken = new();

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, cancellationToken))
                .ReturnsAsync(existingUser);

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<User>(), null, cancellationToken))
                .ReturnsAsync(1);

            // Act
            await _authenticationService.ChangePasswordAsync(changePasswordDto, cancellationToken);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.GetByIdAsync(1, cancellationToken),
                Times.Once);
            _mockUserRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<User>(), null, cancellationToken),
                Times.Once);
        }

        #endregion
    }
}
