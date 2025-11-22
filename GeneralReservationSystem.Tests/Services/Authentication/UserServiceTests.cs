using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.DTOs.Authentication;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;
using GeneralReservationSystem.Application.Services.DefaultImplementations.Authentication;
using Moq;

namespace GeneralReservationSystem.Tests.Services.Authentication
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _userService = new UserService(_mockUserRepository.Object);
        }

        #region GetUserAsync Tests

        [Fact]
        public async Task GetUserAsync_ValidKey_ReturnsUser()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            User expectedUser = new()
            {
                UserId = 1,
                UserName = "testuser",
                Email = "test@example.com",
                IsAdmin = false
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUser);

            // Act
            User result = await _userService.GetUserAsync(keyDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.UserId, result.UserId);
            Assert.Equal(expectedUser.UserName, result.UserName);
            Assert.Equal(expectedUser.Email, result.Email);
            Assert.Equal(expectedUser.IsAdmin, result.IsAdmin);

            _mockUserRepository.Verify(
                repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetUserAsync_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 999 };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _userService.GetUserAsync(keyDto));

            Assert.Equal("No se encontró el usuario solicitado.", exception.Message);
        }

        [Fact]
        public async Task GetUserAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _userService.GetUserAsync(keyDto));

            Assert.Equal("Error al consultar el usuario.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetUserAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            CancellationToken cancellationToken = new();
            User expectedUser = new() { UserId = 1 };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, cancellationToken))
                .ReturnsAsync(expectedUser);

            // Act
            _ = await _userService.GetUserAsync(keyDto, cancellationToken);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.GetByIdAsync(1, cancellationToken),
                Times.Once);
        }

        #endregion

        #region UpdateUserAsync Tests

        [Fact]
        public async Task UpdateUserAsync_UpdateUserNameOnly_ReturnsUpdatedUser()
        {
            // Arrange
            UpdateUserDto updateDto = new()
            {
                UserId = 1,
                UserName = "newusername",
                Email = null
            };

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<User>(),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            User result = await _userService.UpdateUserAsync(updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.UserId, result.UserId);
            Assert.Equal(updateDto.UserName, result.UserName);

            _mockUserRepository.Verify(
                repo => repo.UpdateAsync(
                    It.Is<User>(u => u.UserId == 1 && u.UserName == "newusername"),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_UpdateEmailOnly_ReturnsUpdatedUser()
        {
            // Arrange
            UpdateUserDto updateDto = new()
            {
                UserId = 1,
                UserName = null,
                Email = "newemail@example.com"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<User>(),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            User result = await _userService.UpdateUserAsync(updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.UserId, result.UserId);
            Assert.Equal(updateDto.Email, result.Email);

            _mockUserRepository.Verify(
                repo => repo.UpdateAsync(
                    It.Is<User>(u => u.UserId == 1 && u.Email == "newemail@example.com"),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_UpdateBothUserNameAndEmail_ReturnsUpdatedUser()
        {
            // Arrange
            UpdateUserDto updateDto = new()
            {
                UserId = 1,
                UserName = "newusername",
                Email = "newemail@example.com"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<User>(),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            User result = await _userService.UpdateUserAsync(updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.UserId, result.UserId);
            Assert.Equal(updateDto.UserName, result.UserName);
            Assert.Equal(updateDto.Email, result.Email);

            _mockUserRepository.Verify(
                repo => repo.UpdateAsync(
                    It.Is<User>(u => u.UserId == 1 && u.UserName == "newusername" && u.Email == "newemail@example.com"),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_NoUpdates_ReturnsExistingUser()
        {
            // Arrange
            UpdateUserDto updateDto = new()
            {
                UserId = 1,
                UserName = null,
                Email = null
            };

            User existingUser = new()
            {
                UserId = 1,
                UserName = "existinguser",
                Email = "existing@example.com",
                IsAdmin = false
            };

            _ = _mockUserRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            User result = await _userService.UpdateUserAsync(updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingUser.UserId, result.UserId);
            Assert.Equal(existingUser.UserName, result.UserName);
            Assert.Equal(existingUser.Email, result.Email);

            _mockUserRepository.Verify(
                repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);
            _mockUserRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<Func<User, object?>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateUserAsync_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            UpdateUserDto updateDto = new()
            {
                UserId = 999,
                UserName = "newusername"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<User>(),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _userService.UpdateUserAsync(updateDto));

            Assert.Equal("No se encontró el usuario para actualizar.", exception.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_DuplicateUserNameOrEmail_ThrowsServiceBusinessException()
        {
            // Arrange
            UpdateUserDto updateDto = new()
            {
                UserId = 1,
                UserName = "duplicateuser"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<User>(),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UniqueConstraintViolationException("UQ_User_UserName"));

            // Act & Assert
            ServiceBusinessException exception = await Assert.ThrowsAsync<ServiceBusinessException>(
                () => _userService.UpdateUserAsync(updateDto));

            Assert.Equal("Ya existe un usuario con el mismo nombre o correo electrónico.", exception.Message);
            _ = Assert.IsType<UniqueConstraintViolationException>(exception.InnerException);
        }

        [Fact]
        public async Task UpdateUserAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            UpdateUserDto updateDto = new()
            {
                UserId = 1,
                UserName = "newusername"
            };

            _ = _mockUserRepository
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<User>(),
                    It.IsAny<Func<User, object?>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _userService.UpdateUserAsync(updateDto));

            Assert.Equal("Error al actualizar el usuario.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        #endregion

        #region DeleteUserAsync Tests

        [Fact]
        public async Task DeleteUserAsync_ValidKey_DeletesUser()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };

            _ = _mockUserRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _userService.DeleteUserAsync(keyDto);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.DeleteAsync(
                    It.Is<User>(u => u.UserId == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_ThrowsServiceNotFoundException()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 999 };

            _ = _mockUserRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            ServiceNotFoundException exception = await Assert.ThrowsAsync<ServiceNotFoundException>(
                () => _userService.DeleteUserAsync(keyDto));

            Assert.Equal("No se encontró el usuario para eliminar.", exception.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };

            _ = _mockUserRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _userService.DeleteUserAsync(keyDto));

            Assert.Equal("Error al eliminar el usuario.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task DeleteUserAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            UserKeyDto keyDto = new() { UserId = 1 };
            CancellationToken cancellationToken = new();

            _ = _mockUserRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<User>(), cancellationToken))
                .ReturnsAsync(1);

            // Act
            await _userService.DeleteUserAsync(keyDto, cancellationToken);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.DeleteAsync(It.IsAny<User>(), cancellationToken),
                Times.Once);
        }

        #endregion

        #region GetAllUsersAsync Tests

        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            // Arrange
            List<User> expectedUsers =
            [
                new User { UserId = 1, UserName = "user1", Email = "user1@example.com", IsAdmin = false },
                new User { UserId = 2, UserName = "user2", Email = "user2@example.com", IsAdmin = false },
                new User { UserId = 3, UserName = "admin", Email = "admin@example.com", IsAdmin = true }
            ];

            _ = _mockUserRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUsers);

            // Act
            IEnumerable<User> result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            List<User> userList = [.. result];
            Assert.Equal(3, userList.Count);
            Assert.Equal(expectedUsers[0].UserId, userList[0].UserId);
            Assert.Equal(expectedUsers[1].UserId, userList[1].UserId);
            Assert.Equal(expectedUsers[2].UserId, userList[2].UserId);

            _mockUserRepository.Verify(
                repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllUsersAsync_NoUsers_ReturnsEmptyCollection()
        {
            // Arrange
            _ = _mockUserRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            IEnumerable<User> result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockUserRepository.Verify(
                repo => repo.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllUsersAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            _ = _mockUserRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _userService.GetAllUsersAsync());

            Assert.Equal("Error al obtener la lista de usuarios.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithCancellationToken_PassesTokenToRepository()
        {
            // Arrange
            CancellationToken cancellationToken = new();
            _ = _mockUserRepository
                .Setup(repo => repo.GetAllAsync(cancellationToken))
                .ReturnsAsync([]);

            // Act
            _ = await _userService.GetAllUsersAsync(cancellationToken);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.GetAllAsync(cancellationToken),
                Times.Once);
        }

        #endregion

        #region SearchUsersAsync Tests

        [Fact]
        public async Task SearchUsersAsync_ValidRequest_ReturnsPagedResult()
        {
            // Arrange
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
                    new() {
                        UserId = 1,
                        UserName = "user1",
                        Email = "user1@example.com",
                        IsAdmin = false
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.TotalCount, result.TotalCount);
            Assert.Equal(expectedResult.Page, result.Page);
            Assert.Equal(expectedResult.PageSize, result.PageSize);
            _ = Assert.Single(result.Items);

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_NoResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFilterEquals_ReturnsFilteredResults()
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
                        new Filter("UserId", FilterOperator.Equals, 1)
                    ])
                ],
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

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, item => Assert.Equal(1, item.UserId));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFilterContains_ReturnsFilteredResults()
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
                        new Filter("UserName", FilterOperator.Contains, "admin")
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "admin", Email = "admin@example.com", IsAdmin = true },
                    new() { UserId = 2, UserName = "sysadmin", Email = "sysadmin@example.com", IsAdmin = true }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.Contains("admin", item.UserName.ToLower()));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFilterStartsWith_ReturnsFilteredResults()
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
                        new Filter("Email", FilterOperator.StartsWith, "user")
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "john", Email = "user1@example.com", IsAdmin = false },
                    new() { UserId = 2, UserName = "jane", Email = "user2@example.com", IsAdmin = false }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.StartsWith("user", item.Email));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFilterEndsWith_ReturnsFilteredResults()
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
                        new Filter("Email", FilterOperator.EndsWith, "@company.com")
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "employee1", Email = "emp1@company.com", IsAdmin = false },
                    new() { UserId = 2, UserName = "employee2", Email = "emp2@company.com", IsAdmin = false }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.EndsWith("@company.com", item.Email));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFilterNotEquals_ReturnsFilteredResults()
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
                        new Filter("IsAdmin", FilterOperator.NotEquals, true)
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 2, UserName = "user1", Email = "user1@example.com", IsAdmin = false },
                    new() { UserId = 3, UserName = "user2", Email = "user2@example.com", IsAdmin = false }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.False(item.IsAdmin));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFilterGreaterThan_ReturnsFilteredResults()
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
                        new Filter("UserId", FilterOperator.GreaterThan, 5)
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 6, UserName = "user6", Email = "user6@example.com" },
                    new() { UserId = 7, UserName = "user7", Email = "user7@example.com" }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.UserId > 5));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFilterLessThanOrEqual_ReturnsFilteredResults()
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
                        new Filter("UserId", FilterOperator.LessThanOrEqual, 3)
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "user1", Email = "user1@example.com" },
                    new() { UserId = 2, UserName = "user2", Email = "user2@example.com" },
                    new() { UserId = 3, UserName = "user3", Email = "user3@example.com" }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.UserId <= 3));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFilterNotContains_ReturnsFilteredResults()
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
                        new Filter("Email", FilterOperator.NotContains, "example")
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "user1", Email = "user1@company.com" },
                    new() { UserId = 2, UserName = "user2", Email = "user2@business.com" }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.DoesNotContain("example", item.Email));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithMultipleFiltersInSameClause_ReturnsFilteredResults()
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
                        new Filter("UserName", FilterOperator.Equals, "admin"),
                        new Filter("UserName", FilterOperator.Equals, "superadmin")
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "admin", Email = "admin@example.com", IsAdmin = true },
                    new() { UserId = 2, UserName = "superadmin", Email = "superadmin@example.com", IsAdmin = true }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.UserName == "admin" || item.UserName == "superadmin"));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithMultipleFilterClauses_ReturnsFilteredResults()
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
                        new Filter("IsAdmin", FilterOperator.Equals, true)
                    ]),
                    new FilterClause(
                    [
                        new Filter("Email", FilterOperator.Contains, "@example.com")
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "admin", Email = "admin@example.com", IsAdmin = true }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.True(item.IsAdmin);
                Assert.Contains("@example.com", item.Email);
            });

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithOrderByAscending_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("UserName", SortDirection.Asc)
                ]
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "alice", Email = "alice@example.com" },
                    new() { UserId = 2, UserName = "bob", Email = "bob@example.com" },
                    new() { UserId = 3, UserName = "charlie", Email = "charlie@example.com" }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<UserInfo> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(string.Compare(items[i].UserName, items[i - 1].UserName, StringComparison.Ordinal) >= 0);
            }

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithOrderByDescending_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("UserId", SortDirection.Desc)
                ]
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 3, UserName = "user3", Email = "user3@example.com" },
                    new() { UserId = 2, UserName = "user2", Email = "user2@example.com" },
                    new() { UserId = 1, UserName = "user1", Email = "user1@example.com" }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            List<UserInfo> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(items[i].UserId <= items[i - 1].UserId);
            }

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithMultipleOrders_ReturnsOrderedResults()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders =
                [
                    new SortOption("IsAdmin", SortDirection.Desc),
                    new SortOption("UserName", SortDirection.Asc)
                ]
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "admin", Email = "admin@example.com", IsAdmin = true },
                    new() { UserId = 2, UserName = "superadmin", Email = "superadmin@example.com", IsAdmin = true },
                    new() { UserId = 3, UserName = "alice", Email = "alice@example.com", IsAdmin = false },
                    new() { UserId = 4, UserName = "bob", Email = "bob@example.com", IsAdmin = false }
                ],
                TotalCount = 4,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.TotalCount);
            List<UserInfo> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                if (items[i].IsAdmin && !items[i - 1].IsAdmin)
                {
                    Assert.Fail("IsAdmin should be ordered descending (true before false)");
                }
                if (items[i].IsAdmin == items[i - 1].IsAdmin)
                {
                    Assert.True(string.Compare(items[i].UserName, items[i - 1].UserName, StringComparison.Ordinal) >= 0);
                }
            }

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithFiltersAndOrders_ReturnsFilteredAndOrderedResults()
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
                        new Filter("Email", FilterOperator.Contains, "@example.com")
                    ])
                ],
                Orders =
                [
                    new SortOption("UserName", SortDirection.Asc)
                ]
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "alice", Email = "alice@example.com", IsAdmin = false },
                    new() { UserId = 2, UserName = "bob", Email = "bob@example.com", IsAdmin = false },
                    new() { UserId = 3, UserName = "charlie", Email = "charlie@example.com", IsAdmin = false }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, item => Assert.Contains("@example.com", item.Email));
            List<UserInfo> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                Assert.True(string.Compare(items[i].UserName, items[i - 1].UserName, StringComparison.Ordinal) >= 0);
            }

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_WithComplexFilteringAndOrdering_ReturnsCorrectResults()
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
                        new Filter("IsAdmin", FilterOperator.Equals, true),
                        new Filter("IsAdmin", FilterOperator.Equals, false)
                    ]),
                    new FilterClause(
                    [
                        new Filter("UserId", FilterOperator.GreaterThan, 0)
                    ])
                ],
                Orders =
                [
                    new SortOption("IsAdmin", SortDirection.Desc),
                    new SortOption("Email", SortDirection.Asc)
                ]
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "admin", Email = "admin@example.com", IsAdmin = true },
                    new() { UserId = 2, UserName = "alice", Email = "alice@example.com", IsAdmin = false },
                    new() { UserId = 3, UserName = "bob", Email = "bob@example.com", IsAdmin = false }
                ],
                TotalCount = 3,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.UserId > 0));
            List<UserInfo> items = [.. result.Items];
            for (int i = 1; i < items.Count; i++)
            {
                if (items[i].IsAdmin && !items[i - 1].IsAdmin)
                {
                    Assert.Fail("IsAdmin should be ordered descending");
                }
                if (items[i].IsAdmin == items[i - 1].IsAdmin)
                {
                    Assert.True(string.Compare(items[i].Email, items[i - 1].Email, StringComparison.Ordinal) >= 0);
                }
            }

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_FilterByAdminStatus_ReturnsOnlyAdmins()
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
                        new Filter("IsAdmin", FilterOperator.Equals, true)
                    ])
                ],
                Orders = []
            };

            PagedResult<UserInfo> expectedResult = new()
            {
                Items =
                [
                    new() { UserId = 1, UserName = "admin", Email = "admin@example.com", IsAdmin = true },
                    new() { UserId = 2, UserName = "superadmin", Email = "superadmin@example.com", IsAdmin = true }
                ],
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            PagedResult<UserInfo> result = await _userService.SearchUsersAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.True(item.IsAdmin));

            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_RepositoryError_ThrowsServiceException()
        {
            // Arrange
            PagedSearchRequestDto searchDto = new()
            {
                Page = 1,
                PageSize = 10,
                FilterClauses = [],
                Orders = []
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RepositoryException("Database error"));

            // Act & Assert
            ServiceException exception = await Assert.ThrowsAsync<ServiceException>(
                () => _userService.SearchUsersAsync(searchDto));

            Assert.Equal("Error al buscar usuarios.", exception.Message);
            _ = Assert.IsType<RepositoryException>(exception.InnerException);
        }

        [Fact]
        public async Task SearchUsersAsync_WithCancellationToken_PassesTokenToRepository()
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
            PagedResult<UserInfo> expectedResult = new()
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            };

            _ = _mockUserRepository
                .Setup(repo => repo.SearchWithInfoAsync(searchDto, cancellationToken))
                .ReturnsAsync(expectedResult);

            // Act
            _ = await _userService.SearchUsersAsync(searchDto, cancellationToken);

            // Assert
            _mockUserRepository.Verify(
                repo => repo.SearchWithInfoAsync(searchDto, cancellationToken),
                Times.Once);
        }

        #endregion
    }
}
