using HealthForumApi.Dtos;
using HealthForumApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthForumApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly ILogger<PostsController> _logger;

        public PostsController(IPostService postService, ILogger<PostsController> logger)
        {
            _postService = postService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]//jwt vaidation
        public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
        {
            _logger.LogInformation("Received post creation request");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt: User ID not found in token");
                return Unauthorized("User ID not found in token.");
            }

            dto.AuthorId = userId;
            try
            {
                var post = await _postService.CreatePostAsync(dto);
                _logger.LogInformation("Post created successfully, postId: {PostId}", post.Id);
                return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create post");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string tag = null)
        {
            _logger.LogInformation("Received request to retrieve posts, tag: {Tag}", tag ?? "none");
            try
            {
                var posts = await _postService.GetPostsAsync(tag);
                _logger.LogInformation("Retrieved {PostCount} posts", posts?.Count ?? 0);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve posts");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            _logger.LogInformation("Received request to retrieve post, postId: {PostId}", id);
            try
            {
                var post = await _postService.GetPostByIdAsync(id);
                if (post == null)
                {
                    _logger.LogWarning("Post not found, postId: {PostId}", id);
                    return NotFound();
                }
                _logger.LogInformation("Post retrieved successfully, postId: {PostId}", id);
                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve post, postId: {PostId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePostDto dto)
        {
            _logger.LogInformation("Received post update request for postId: {PostId}", id);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt: User ID not found in token for postId: {PostId}", id);
                return Unauthorized("User ID not found in token.");
            }

            try
            {
                var existingPost = await _postService.GetPostByIdAsync(id);
                if (existingPost == null)
                {
                    _logger.LogWarning("Post not found for update, postId: {PostId}", id);
                    return NotFound("Post not found.");
                }

                if (existingPost.AuthorId != userId)
                {
                    _logger.LogWarning("Forbidden access: User {UserId} attempted to update post {PostId} not owned by them", userId, id);
                    return Forbid("You can only edit your own posts.");
                }

                dto.Id = id;
                dto.AuthorId = userId;
                var updatedPost = await _postService.UpdatePostAsync(dto);
                _logger.LogInformation("Post updated successfully, postId: {PostId}", id);
                return Ok(updatedPost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update post, postId: {PostId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<IActionResult> LikePost(string id)
        {
            _logger.LogInformation("Received like request for postId: {PostId}", id);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt: User ID not found in token for postId: {PostId}", id);
                return Unauthorized("User ID not found in token.");
            }

            try
            {
                await _postService.UpdatePostReactionAsync(id, userId, "like");
                var post = await _postService.GetPostByIdAsync(id);
                _logger.LogInformation("Post liked successfully, postId: {PostId}, userId: {UserId}", id, userId);
                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to like post, postId: {PostId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/dislike")]
        [Authorize]
        public async Task<IActionResult> DislikePost(string id)
        {
            _logger.LogInformation("Received dislike request for postId: {PostId}", id);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt: User ID not found in token for postId: {PostId}", id);
                return Unauthorized("User ID not found in token.");
            }

            try
            {
                await _postService.UpdatePostReactionAsync(id, userId, "dislike");
                var post = await _postService.GetPostByIdAsync(id);
                _logger.LogInformation("Post disliked successfully, postId: {PostId}, userId: {UserId}", id, userId);
                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dislike post, postId: {PostId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation("Received post deletion request for postId: {PostId}", id);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt: User ID not found in token for postId: {PostId}", id);
                return Unauthorized("User ID not found in token.");
            }

            try
            {
                var post = await _postService.GetPostByIdAsync(id);
                if (post == null)
                {
                    _logger.LogWarning("Post not found for deletion, postId: {PostId}", id);
                    return NotFound("Post not found.");
                }

                if (post.AuthorId != userId)
                {
                    _logger.LogWarning("Forbidden access: User {UserId} attempted to delete post {PostId} not owned by them", userId, id);
                    return Forbid("You can only delete your own posts.");
                }

                await _postService.DeletePostAsync(id);
                _logger.LogInformation("Post deleted successfully, postId: {PostId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete post, postId: {PostId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}