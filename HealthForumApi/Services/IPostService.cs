using HealthForumApi.Dtos;
using HealthForumApi.Models.HealthForumApi.Models;

namespace HealthForumApi.Services
{
    public interface IPostService
    {
        Task<Post> CreatePostAsync(CreatePostDto dto);
        Task<Post> GetPostByIdAsync(string id);
        Task<List<Post>> GetPostsAsync(string tag = null);
        Task UpdatePostReactionAsync(string id, string userId, string reactionType); // New method
        Task DeletePostAsync(string id); // Add this method
        Task<Post> UpdatePostAsync(UpdatePostDto dto);
    }
}
