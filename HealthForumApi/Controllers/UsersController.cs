using HealthForumApi.Dtos;
using HealthForumApi.Models;
using HealthForumApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthForumApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            _logger.LogInformation("Received registration request for email: {Email}", dto.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration data for email: {Email}", dto.Email);
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _userService.RegisterAsync(dto.Email, dto.Password, dto.Username);
                _logger.LogInformation("User registered successfully, userId: {UserId}", user.Id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register user for email: {Email}", dto.Email);
                return BadRequest("Registration failed. Please try again.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            _logger.LogInformation("Received login request for email: {Email}", dto.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login data for email: {Email}", dto.Email);
                return BadRequest(ModelState);
            }

            try
            {
                var token = await _userService.LoginAsync(dto.Email, dto.Password);
                _logger.LogInformation("User logged in successfully, email: {Email}", dto.Email);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed login attempt for email: {Email}. Reason: {Message}", dto.Email, ex.Message);
                return BadRequest("Login failed. Please check your credentials and try again.");
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(string id)
        {
            _logger.LogInformation("Received request to retrieve user, userId: {UserId}", id);

            var authenticatedUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authenticatedUserId))
            {
                _logger.LogWarning("Unauthorized access attempt: User ID not found in token for requested userId: {UserId}", id);
                return Unauthorized("User ID not found in token.");
            }

            if (authenticatedUserId != id)
            {
                _logger.LogWarning("Forbidden access: User {AuthenticatedUserId} attempted to access user {UserId}", authenticatedUserId, id);
                return Forbid("You can only access your own user data.");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User not found, userId: {UserId}", id);
                    return NotFound("User not found.");
                }

                _logger.LogInformation("User retrieved successfully, userId: {UserId}", id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user, userId: {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}