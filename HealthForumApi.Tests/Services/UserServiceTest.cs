using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using HealthForumApi.Models;
using HealthForumApi.Services;
using HealthForumApi.Tests.TestData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthForumApi.Tests.Services
{
    public class UserServiceTest
    {
        private readonly Mock<IDynamoDBContext> _dbContextMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _service;

        public UserServiceTest()
        {
            _dbContextMock = new Mock<IDynamoDBContext>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<UserService>>();

            // Configure IConfiguration for JWT settings
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(s => s.Value).Returns("test-secret-key");
            _configMock.Setup(c => c["Jwt:SecretKey"]).Returns("test-secret-key");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("test-audience");

            _service = new UserService(_dbContextMock.Object, _configMock.Object, _loggerMock.Object);
        }

        // Tests for RegisterAsync
        [Fact]
        public async Task RegisterAsync_ValidInput_ReturnsUser()
        {
            // Arrange
            var email = "test@example.com";
            var password = "password123";
            var username = "testuser";
            var asyncSearch = new Mock<IAsyncSearch<User>>();
            asyncSearch.Setup(s => s.GetRemainingAsync(default)).ReturnsAsync(new List<User>());
            _dbContextMock.Setup(db => db.QueryAsync<User>(email, It.Is<DynamoDBOperationConfig>(c => c.IndexName == "EmailIndex")))
                .Returns(asyncSearch.Object);
            _dbContextMock.Setup(db => db.SaveAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RegisterAsync(email, password, username);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(email);
            result.Username.Should().Be(username);
            BCrypt.Net.BCrypt.Verify(password, result.PasswordHash).Should().BeTrue();
            _dbContextMock.Verify(db => db.SaveAsync(It.Is<User>(u => u.Id == result.Id), default), Times.Once());
        }

        [Fact]
        public async Task RegisterAsync_ExistingEmail_ThrowsException()
        {
            // Arrange
            var email = "test@example.com";
            var password = "password123";
            var username = "testuser";
            var id = "1234";
            var existingUser = TestDataGenerator.User(id, email, BCrypt.Net.BCrypt.HashPassword("existing"), "existinguser");
            var asyncSearch = new Mock<IAsyncSearch<User>>();
            asyncSearch.Setup(s => s.GetRemainingAsync(default)).ReturnsAsync(new List<User> { existingUser });
            _dbContextMock.Setup(db => db.QueryAsync<User>(email, It.Is<DynamoDBOperationConfig>(c => c.IndexName == "EmailIndex")))
                .Returns(asyncSearch.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(email, password, username));
            _dbContextMock.Verify(db => db.SaveAsync(It.IsAny<User>(), default), Times.Never());
        }

        
        [Fact]
        public async Task RegisterAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var email = "test@example.com";
            var password = "password123";
            var username = "testuser";
            var asyncSearch = new Mock<IAsyncSearch<User>>();
            asyncSearch.Setup(s => s.GetRemainingAsync(default)).ReturnsAsync(new List<User>());
            _dbContextMock.Setup(db => db.QueryAsync<User>(email, It.Is<DynamoDBOperationConfig>(c => c.IndexName == "EmailIndex")))
                .Returns(asyncSearch.Object);
            _dbContextMock.Setup(db => db.SaveAsync(It.IsAny<User>(), default))
                .ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(email, password, username));
        }

        // Tests for LoginAsync
        
        [Fact]
        public async Task LoginAsync_InvalidEmail_ThrowsException()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "password123";
            var asyncSearch = new Mock<IAsyncSearch<User>>();
            asyncSearch.Setup(s => s.GetRemainingAsync(default)).ReturnsAsync(new List<User>());
            _dbContextMock.Setup(db => db.QueryAsync<User>(email, It.Is<DynamoDBOperationConfig>(c => c.IndexName == "EmailIndex")))
                .Returns(asyncSearch.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.LoginAsync(email, password));
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsException()
        {
            // Arrange
            var email = "test@example.com";
            var password = "wrongpassword";
            var user = TestDataGenerator.User("user1", email, BCrypt.Net.BCrypt.HashPassword("password123"), "testuser");
            var asyncSearch = new Mock<IAsyncSearch<User>>();
            asyncSearch.Setup(s => s.GetRemainingAsync(default)).ReturnsAsync(new List<User> { user });
            _dbContextMock.Setup(db => db.QueryAsync<User>(email, It.Is<DynamoDBOperationConfig>(c => c.IndexName == "EmailIndex")))
                .Returns(asyncSearch.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.LoginAsync(email, password));
        }

        
        // Tests for GetUserByIdAsync
        [Fact]
        public async Task GetUserByIdAsync_UserExists_ReturnsUser()
        {
            // Arrange
            var id = "user1";
            var user = TestDataGenerator.User(id, "test@example.com", BCrypt.Net.BCrypt.HashPassword("password123"), "testuser");
            _dbContextMock.Setup(db => db.LoadAsync<User>(id, default)).ReturnsAsync(user);

            // Act
            var result = await _service.GetUserByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(user);
        }

        [Fact]
        public async Task GetUserByIdAsync_UserNotFound_ReturnsNull()
        {
            // Arrange
            var id = "nonexistent";
            _dbContextMock.Setup(db => db.LoadAsync<User>(id, default)).ReturnsAsync((User)null);

            // Act
            var result = await _service.GetUserByIdAsync(id);

            // Assert
            result.Should().BeNull();
        }

       
        [Fact]
        public async Task GetUserByIdAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var id = "user1";
            _dbContextMock.Setup(db => db.LoadAsync<User>(id, default))
                .ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetUserByIdAsync(id));
        }
    }
}