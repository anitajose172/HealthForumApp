using HealthForumApi.Dtos;
using HealthForumApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthForumApi.Controllers
{
    [ApiController]
    [Route("api/posts/{postId}/[controller]")]
    public class CommentsController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentsController> _logger;
        public CommentsController(ICommentService commentService, ILogger<CommentsController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(string postId, [FromBody] CreateCommentDto dto)
        {
            _logger.LogInformation("Received comment creation request for postId: {PostId}", postId);
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized access attempt: User ID not found in token for postId: {PostId}", postId);
                return Unauthorized("User ID not found in token.");
            }
            dto.AuthorId = userId;
            dto.PostId = postId;
            var comment = await _commentService.CreateCommentAsync(dto);
            return CreatedAtAction(nameof(GetByPostId), new { postId = comment.PostId }, comment);
        }

        [HttpGet]
        public async Task<IActionResult> GetByPostId(string postId)
        {
            var comments = await _commentService.GetCommentsByPostIdAsync(postId);
            return Ok(comments);
        }

        [HttpDelete("{commentId}")]
        [Authorize]
        public async Task<IActionResult> Delete(string postId, string commentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }
            var comment = await _commentService.GetCommentByIdAsync(commentId);
            if (comment == null || comment.PostId != postId)
            {
                return NotFound("Comment not found.");
            }
            if (comment.AuthorId != userId)
            {
                return Forbid("You can only delete your own comments.");
            }
            await _commentService.DeleteCommentAsync(postId, commentId);
            return NoContent();
        }
    }
}