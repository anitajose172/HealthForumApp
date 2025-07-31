using Amazon.DynamoDBv2.DataModel;
using HealthForumApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HealthForumApi.Services
{
    public class UserService : IUserService
    {
        private readonly IDynamoDBContext _dbContext;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly ILogger<UserService> _logger;

        public UserService(IDynamoDBContext dbContext, IConfiguration configuration, ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _secretKey = configuration["Jwt:SecretKey"];
            _issuer = configuration["Jwt:Issuer"];
            _audience = configuration["Jwt:Audience"];
            _logger = logger;
        }

        public async Task<User> RegisterAsync(string email, string password, string username)
        {
            _logger.LogInformation("RegisterAsync called with email: {Email}, username: {Username}", email, username);
            var existingUsers = await _dbContext.QueryAsync<User>(email, new DynamoDBOperationConfig
            {
                IndexName = "EmailIndex"
            }).GetRemainingAsync();

            if (existingUsers.Any())
                throw new Exception("Email already exists");

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Username = username,
                Bio = ""
            };

            await _dbContext.SaveAsync(user);
            return user;
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            _logger.LogInformation("LoginAsync called with email: {Email}", email);

            var users = await _dbContext.QueryAsync<User>(email, new DynamoDBOperationConfig
            {
                IndexName = "EmailIndex"
            }).GetRemainingAsync();

            if (!users.Any())
            {
                _logger.LogWarning("Login failed: Invalid email {Email}", email);
                throw new Exception("Invalid email or password");
            }

            var user = users.First();

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for email {Email}", email);
                throw new Exception("Invalid email or password");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            _logger.LogInformation("Login successful for user: {UserId}", user.Id);
            return tokenHandler.WriteToken(token);
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            return await _dbContext.LoadAsync<User>(id);
        }
    }
}
