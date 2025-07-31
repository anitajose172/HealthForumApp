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
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace HealthForumApi.Tests.Controllers
{
    public class CommentsControllerTests
    {
        private readonly Mock<ICommentService> _commentServiceMock;
        private readonly Mock<ILogger<CommentsController>> _loggerMock;
        private readonly CommentsController _controller;

        public CommentsControllerTests()
        {
            _commentServiceMock = new Mock<ICommentService>();
            _loggerMock = new Mock<ILogger<CommentsController>>();
            _controller = new CommentsController(_commentServiceMock.Object, _loggerMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // Tests for Create (POST /api/posts/{postId}/comments)
        [Fact]
        public async Task Create_ValidComment_ReturnsCreatedAtAction()
        {
            // Arrange
            var postId = "post1";
            var dto = TestDataGenerator.CreateCommentDto(postId: postId, authorId: "user1");
            var comment = TestDataGenerator.Comment(postId: postId, authorId: "user1");
            _commentServiceMock.Setup(s => s.CreateCommentAsync(It.IsAny<CreateCommentDto>())).ReturnsAsync(comment);

            // Act
            var result = await _controller.Create(postId, dto);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>()
                .Which.Should().Match<CreatedAtActionResult>(r =>
                    r.ActionName == nameof(_controller.GetByPostId) &&
                    r.RouteValues["postId"].ToString() == postId &&
                    r.Value.As<Comment>().Id == comment.Id);
        }

        [Fact]
        public async Task Create_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var postId = "post1";
            var dto = TestDataGenerator.CreateCommentDto(postId: postId);
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.Create(postId, dto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("User ID not found in token.");
        }

        // Tests for GetByPostId (GET /api/posts/{postId}/comments)
        [Fact]
        public async Task GetByPostId_CommentsExist_ReturnsOk()
        {
            // Arrange
            var postId = "post1";
            var comments = new List<Comment>
            {
                TestDataGenerator.Comment(id: "comment1", postId: postId),
                TestDataGenerator.Comment(id: "comment2", postId: postId)
            };
            _commentServiceMock.Setup(s => s.GetCommentsByPostIdAsync(postId)).ReturnsAsync(comments);

            // Act
            var result = await _controller.GetByPostId(postId);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<List<Comment>>().Should().BeEquivalentTo(comments);
        }

        [Fact]
        public async Task GetByPostId_NoComments_ReturnsOkWithEmptyList()
        {
            // Arrange
            var postId = "post1";
            var comments = new List<Comment>();
            _commentServiceMock.Setup(s => s.GetCommentsByPostIdAsync(postId)).ReturnsAsync(comments);

            // Act
            var result = await _controller.GetByPostId(postId);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<List<Comment>>().Should().BeEmpty();
        }

        // Tests for Delete (DELETE /api/posts/{postId}/comments/{commentId})
        [Fact]
        public async Task Delete_ValidComment_ReturnsNoContent()
        {
            // Arrange
            var postId = "post1";
            var commentId = "comment1";
            var comment = TestDataGenerator.Comment(id: commentId, postId: postId, authorId: "user1");
            _commentServiceMock.Setup(s => s.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);
            _commentServiceMock.Setup(s => s.DeleteCommentAsync(postId, commentId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(postId, commentId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var postId = "post1";
            var commentId = "comment1";
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.Delete(postId, commentId);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("User ID not found in token.");
        }

        [Fact]
        public async Task Delete_CommentNotFound_ReturnsNotFound()
        {
            // Arrange
            var postId = "post1";
            var commentId = "comment1";
            _commentServiceMock.Setup(s => s.GetCommentByIdAsync(commentId)).ReturnsAsync((Comment)null);

            // Act
            var result = await _controller.Delete(postId, commentId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("Comment not found.");
        }

        [Fact]
        public async Task Delete_PostIdMismatch_ReturnsNotFound()
        {
            // Arrange
            var postId = "post1";
            var commentId = "comment1";
            var comment = TestDataGenerator.Comment(id: commentId, postId: "differentPost", authorId: "user1");
            _commentServiceMock.Setup(s => s.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);

            // Act
            var result = await _controller.Delete(postId, commentId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("Comment not found.");
        }

        [Fact]
        public async Task Delete_ForbiddenUser_ReturnsForbidden()
        {
            // Arrange
            var postId = "post1";
            var commentId = "comment1";
            var comment = TestDataGenerator.Comment(id: commentId, postId: postId, authorId: "user2");
            _commentServiceMock.Setup(s => s.GetCommentByIdAsync(commentId)).ReturnsAsync(comment);

            // Act
            var result = await _controller.Delete(postId, commentId);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }
    }
}