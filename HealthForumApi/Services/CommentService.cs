using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using HealthForumApi.Dtos;
using HealthForumApi.Models;

namespace HealthForumApi.Services
{
    public class CommentService : ICommentService
    {
        private readonly IDynamoDBContext _dbContext;

        public CommentService(IDynamoDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Comment> CreateCommentAsync(CreateCommentDto dto)
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                PostId = dto.PostId,
                AuthorId = dto.AuthorId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };
            await _dbContext.SaveAsync(comment);
            return comment;
        }

        public async Task<List<Comment>> GetCommentsByPostIdAsync(string postId)
        {
            if (postId == null) throw new ArgumentNullException(nameof(postId));
            var conditions = new List<ScanCondition>
    {
        new ScanCondition("PostId", ScanOperator.Equal, postId)
    };
            return await _dbContext.ScanAsync<Comment>(conditions).GetRemainingAsync();
        }

        public async Task<Comment> GetCommentByIdAsync(string commentId)
        {
            return await _dbContext.LoadAsync<Comment>(commentId);
        }

        public async Task DeleteCommentAsync(string postId, string commentId)
        {
            if (postId == null) throw new ArgumentNullException(nameof(postId));
            if (commentId == null) throw new ArgumentNullException(nameof(commentId));
            var comment = await _dbContext.LoadAsync<Comment>(commentId);
            if (comment != null && comment.PostId == postId)
            {
                await _dbContext.DeleteAsync(comment);
            }
        }
    }
}