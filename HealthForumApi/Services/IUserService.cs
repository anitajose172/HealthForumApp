using HealthForumApi.Models;

namespace HealthForumApi.Services
{
    public interface IUserService
    {
        Task<User> RegisterAsync(string email, string password, string username);
        Task<string> LoginAsync(string email, string password);
        Task<User> GetUserByIdAsync(string id);
    }
}
