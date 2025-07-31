using HealthForumApi.Dtos;
using HealthForumApi.Models;

namespace HealthForumApi.Services
{
    public interface ICommentService
    {
        Task<Comment> CreateCommentAsync(CreateCommentDto dto);
        Task<List<Comment>> GetCommentsByPostIdAsync(string postId);
        Task<Comment> GetCommentByIdAsync(string commentId);
        Task DeleteCommentAsync(string postId, string commentId);
    }
}