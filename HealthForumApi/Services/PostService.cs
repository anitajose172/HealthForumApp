using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using HealthForumApi.Dtos;
using HealthForumApi.Models.HealthForumApi.Models;

namespace HealthForumApi.Services
{
    public class PostService : IPostService
    {
        private readonly IDynamoDBContext _dbContext;
        private readonly ILogger<PostService> _logger;

        public PostService(IDynamoDBContext dbContext, ILogger<PostService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Post> CreatePostAsync(CreatePostDto dto)
        {
            _logger.LogInformation("Creating new post for authorId: {AuthorId}", dto.AuthorId);

            var post = new Post
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title,
                Content = dto.Content,
                AuthorId = dto.AuthorId,
                CreatedAt = DateTime.UtcNow,
                Tags = dto.Tags != null ? dto.Tags.ToList() : new List<string>(),
                Likes = 0,
                Dislikes = 0
            };
            try
            {
                await _dbContext.SaveAsync(post);
                _logger.LogInformation("Post created successfully, postId: {PostId}", post.Id);
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create post for authorId: {AuthorId}", dto.AuthorId);
                throw;
            }
        }

        public async Task<Post> GetPostByIdAsync(string id)
        {
            _logger.LogInformation("Retrieving post, postId: {PostId}", id);
            try
            {
                var post = await _dbContext.LoadAsync<Post>(id);
                if (post == null)
                {
                    _logger.LogWarning("Post not found, postId: {PostId}", id);
                }
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve post, postId: {PostId}", id);
                throw;
            }
        }

        public async Task<List<Post>> GetPostsAsync(string tag = null)
        {
            _logger.LogInformation("Retrieving posts, tag: {Tag}", tag ?? "none");
            try
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return await _dbContext.ScanAsync<Post>(new List<ScanCondition>()).GetRemainingAsync();
                }
                else
                {
                    var conditions = new List<ScanCondition>
                    {
                        new ScanCondition("Tags", ScanOperator.Contains, tag)
                    };
                    return await _dbContext.ScanAsync<Post>(conditions).GetRemainingAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve posts, tag: {Tag}", tag ?? "none");
                throw;
            }
        }

        public async Task UpdatePostReactionAsync(string id, string userId, string reactionType)
        {
            _logger.LogInformation("Updating reaction for postId: {PostId}, userId: {UserId}, reactionType: {ReactionType}", id, userId, reactionType);
            try
            {
                var post = await _dbContext.LoadAsync<Post>(id);
                if (post != null)
                {
                    if (reactionType == "like")
                    {
                        if (post.Likes > 0 && post.Dislikes == 0) post.Likes -= 1;
                        else if (post.Dislikes > 0) { post.Dislikes -= 1; post.Likes += 1; }
                        else post.Likes += 1;
                    }
                    else if (reactionType == "dislike")
                    {
                        if (post.Dislikes > 0 && post.Likes == 0) post.Dislikes -= 1;
                        else if (post.Likes > 0) { post.Likes -= 1; post.Dislikes += 1; }
                        else post.Dislikes += 1;
                    }
                    await _dbContext.SaveAsync(post);
                    _logger.LogInformation("Reaction updated successfully, postId: {PostId}", id);
                }
                else
                {
                    _logger.LogWarning("Post not found for reaction update, postId: {PostId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update reaction for postId: {PostId}", id);
                throw;
            }
        }

        public async Task DeletePostAsync(string id)
        {
            _logger.LogInformation("Deleting post, postId: {PostId}", id);
            try
            {
                var post = await _dbContext.LoadAsync<Post>(id);
                if (post != null)
                {
                    await _dbContext.DeleteAsync(post);
                    _logger.LogInformation("Post deleted successfully, postId: {PostId}", id);
                }
                else
                {
                    _logger.LogWarning("Post not found for deletion, postId: {PostId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete post, postId: {PostId}", id);
                throw;
            }
        }

        public async Task<Post> UpdatePostAsync(UpdatePostDto dto)
        {
            _logger.LogInformation("Updating post, postId: {PostId}", dto.Id);

            if (string.IsNullOrEmpty(dto.Id))
            {
                _logger.LogWarning("Invalid update request: Post ID is null or empty");
                throw new ArgumentException("Post ID is required.", nameof(dto.Id));
            }

            try
            {
                var existingPost = await _dbContext.LoadAsync<Post>(dto.Id);
                if (existingPost == null)
                {
                    _logger.LogWarning("Post not found for update, postId: {PostId}", dto.Id);
                    return null;
                }

                existingPost.Title = dto.Title ?? existingPost.Title;
                existingPost.Content = dto.Content ?? existingPost.Content;

                await _dbContext.SaveAsync(existingPost);
                _logger.LogInformation("Post updated successfully, postId: {PostId}", dto.Id);
                return existingPost;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update post, postId: {PostId}", dto.Id);
                throw;
            }
        }
    }
}