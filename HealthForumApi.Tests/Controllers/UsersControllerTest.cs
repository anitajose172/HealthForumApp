using FluentAssertions;
using HealthForumApi.Controllers;
using HealthForumApi.Dtos;
using HealthForumApi.Models;
using HealthForumApi.Services;
using HealthForumApi.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace HealthForumApi.Tests.Controllers
{
    public class UsersControllerTest
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<UsersController>> _loggerMock;
        private readonly UsersController _controller;

        public UsersControllerTest()
        {
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<UsersController>>();
            _controller = new UsersController(_userServiceMock.Object, _loggerMock.Object);

            // Mock user claims for authorization
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // Tests for Register (POST /api/users/register)
        [Fact]
        public async Task Register_ValidDto_ReturnsOk()
        {
            // Arrange
            var dto = TestDataGenerator.RegisterDto();
            var user = TestDataGenerator.User();
            _userServiceMock.Setup(s => s.RegisterAsync(dto.Email, dto.Password, dto.Username)).ReturnsAsync(user);

            // Act
            var result = await _controller.Register(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<User>().Should().BeEquivalentTo(user);
        }

        [Fact]
        public async Task Register_InvalidDto_ReturnsBadRequest()
        {
            // Arrange
            var dto = new RegisterDto { Email = "invalid-email", Password = "", Username = "" };
            _controller.ModelState.AddModelError("Email", "Invalid email format");
            _controller.ModelState.AddModelError("Password", "Password is required");

            // Act
            var result = await _controller.Register(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Register_DuplicateEmail_ThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var dto = TestDataGenerator.RegisterDto();
            _userServiceMock.Setup(s => s.RegisterAsync(dto.Email, dto.Password, dto.Username))
                .ThrowsAsync(new Exception("Email already exists"));

            // Act
            var result = await _controller.Register(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Registration failed. Please try again.");
        }

        // Tests for Login (POST /api/users/login)
        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var dto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
            var token = "mock-token";
            _userServiceMock.Setup(s => s.LoginAsync(dto.Email, dto.Password)).ReturnsAsync(token);

            // Act
            var result = await _controller.Login(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(new { Token = token });
        }

        [Fact]
        public async Task Login_InvalidDto_ReturnsBadRequest()
        {
            // Arrange
            var dto = new LoginDto { Email = "", Password = "" };
            _controller.ModelState.AddModelError("Email", "Email is required");
            _controller.ModelState.AddModelError("Password", "Password is required");

            // Act
            var result = await _controller.Login(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Login_InvalidCredentials_ThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var dto = new LoginDto { Email = "test@example.com", Password = "WrongPassword" };
            _userServiceMock.Setup(s => s.LoginAsync(dto.Email, dto.Password))
                .ThrowsAsync(new Exception("Invalid email or password"));

            // Act
            var result = await _controller.Login(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Login failed. Please check your credentials and try again.");
        }

        // Tests for GetUser (GET /api/users/{id})
        [Fact]
        public async Task GetUser_AuthorizedUser_ReturnsOk()
        {
            // Arrange
            var id = "user1";
            var user = TestDataGenerator.User(id: id);
            _userServiceMock.Setup(s => s.GetUserByIdAsync(id)).ReturnsAsync(user);

            // Act
            var result = await _controller.GetUser(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<User>().Should().BeEquivalentTo(user);
        }

        [Fact]
        public async Task GetUser_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var id = "user1";
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.GetUser(id);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("User ID not found in token.");
        }

        [Fact]
        public async Task GetUser_ForbiddenUser_ReturnsForbidden()
        {
            // Arrange
            var id = "user2"; // Different user
            _userServiceMock.Setup(s => s.GetUserByIdAsync(id)).ReturnsAsync(TestDataGenerator.User(id: id));

            // Act
            var result = await _controller.GetUser(id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetUser_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = "user1";
            _userServiceMock.Setup(s => s.GetUserByIdAsync(id)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetUser(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("User not found.");
        }
    }
}
