using FluentAssertions;
using HealthForumApi.Controllers;
using HealthForumApi.Dtos;
using HealthForumApi.Models.HealthForumApi.Models;
using HealthForumApi.Services;
using HealthForumApi.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace HealthForumApi.Tests.Controllers
{
    public class PostsControllerTest
    {
        private readonly Mock<IPostService> _postServiceMock;
        private readonly Mock<ILogger<PostsController>> _loggerMock;
        private readonly PostsController _controller;

        public PostsControllerTest()
        {
            _postServiceMock = new Mock<IPostService>();
            _loggerMock = new Mock<ILogger<PostsController>>();
            _controller = new PostsController(_postServiceMock.Object, _loggerMock.Object);

            // Mock user claims for authorization
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // Tests for Create (POST /api/posts)
        [Fact]
        public async Task Create_ValidPost_ReturnsCreatedAtAction()
        {
            // Arrange
            var dto = TestDataGenerator.CreatePostDto(authorId: "user1");
            var post = TestDataGenerator.Post(authorId: "user1");
            _postServiceMock.Setup(s => s.CreatePostAsync(It.IsAny<CreatePostDto>())).ReturnsAsync(post);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>()
                .Which.Should().Match<CreatedAtActionResult>(r =>
                    r.ActionName == nameof(_controller.GetById) &&
                    r.RouteValues["id"].ToString() == post.Id &&
                    r.Value.As<Post>().Id == post.Id);
        }

        [Fact]
        public async Task Create_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var dto = TestDataGenerator.CreatePostDto();
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.Create(dto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("User ID not found in token.");
        }

        // Tests for GetAll (GET /api/posts)
        [Fact]
        public async Task GetAll_NoTag_ReturnsOk()
        {
            // Arrange
            var posts = new List<Post>
            {
                TestDataGenerator.Post(id: "post1"),
                TestDataGenerator.Post(id: "post2")
            };
            _postServiceMock.Setup(s => s.GetPostsAsync(null)).ReturnsAsync(posts);

            // Act
            var result = await _controller.GetAll();

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<List<Post>>().Should().BeEquivalentTo(posts);
        }

        [Fact]
        public async Task GetAll_WithTag_ReturnsFilteredPosts()
        {
            // Arrange
            var tag = "test";
            var posts = new List<Post>
            {
                TestDataGenerator.Post(id: "post1")
            };
            _postServiceMock.Setup(s => s.GetPostsAsync(tag)).ReturnsAsync(posts);

            // Act
            var result = await _controller.GetAll(tag);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<List<Post>>().Should().BeEquivalentTo(posts);
        }

        // Tests for GetById (GET /api/posts/{id})
        [Fact]
        public async Task GetById_PostExists_ReturnsOk()
        {
            // Arrange
            var post = TestDataGenerator.Post(id: "post1");
            _postServiceMock.Setup(s => s.GetPostByIdAsync(post.Id)).ReturnsAsync(post);

            // Act
            var result = await _controller.GetById(post.Id);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<Post>().Should().BeEquivalentTo(post);
        }

        [Fact]
        public async Task GetById_PostNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = "nonexistent";
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync((Post)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        // Tests for Update (PUT /api/posts/{id})
        [Fact]
        public async Task Update_ValidPost_ReturnsOk()
        {
            // Arrange
            var id = "post1";
            var userId = "user1";
            var existingPost = TestDataGenerator.Post(id: id, authorId: userId);
            var updatedPost = TestDataGenerator.Post(id: id, authorId: userId);
            var dto = new UpdatePostDto
            {
                Id = id,
                Title = "Updated Title",
                Content = "Updated Content",
            };
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync(existingPost);
            _postServiceMock.Setup(s => s.UpdatePostAsync(It.IsAny<UpdatePostDto>())).ReturnsAsync(updatedPost);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<Post>().Should().BeEquivalentTo(updatedPost);
        }

        [Fact]
        public async Task Update_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var id = "post1";
            var dto = new UpdatePostDto();
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("User ID not found in token.");
        }

        [Fact]
        public async Task Update_PostNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = "nonexistent";
            var dto = new UpdatePostDto();
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync((Post)null);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("Post not found.");
        }

        [Fact]
        public async Task Update_ForbiddenUser_ReturnsForbidden()
        {
            // Arrange
            var id = "post1";
            var userId = "user1";
            var existingPost = TestDataGenerator.Post(id: id, authorId: "user2");
            var dto = new UpdatePostDto();
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync(existingPost);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        // Tests for LikePost (POST /api/posts/{id}/like)
        [Fact]
        public async Task LikePost_ValidPost_ReturnsOk()
        {
            // Arrange
            var id = "post1";
            var userId = "user1";
            var post = TestDataGenerator.Post(id: id);
            _postServiceMock.Setup(s => s.UpdatePostReactionAsync(id, userId, "like")).Returns(Task.CompletedTask);
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync(post);

            // Act
            var result = await _controller.LikePost(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<Post>().Should().BeEquivalentTo(post);
        }

        [Fact]
        public async Task LikePost_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var id = "post1";
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.LikePost(id);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("User ID not found in token.");
        }

        // Tests for DislikePost (POST /api/posts/{id}/dislike)
        [Fact]
        public async Task DislikePost_ValidPost_ReturnsOk()
        {
            // Arrange
            var id = "post1";
            var userId = "user1";
            var post = TestDataGenerator.Post(id: id);
            _postServiceMock.Setup(s => s.UpdatePostReactionAsync(id, userId, "dislike")).Returns(Task.CompletedTask);
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync(post);

            // Act
            var result = await _controller.DislikePost(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<Post>().Should().BeEquivalentTo(post);
        }

        [Fact]
        public async Task DislikePost_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var id = "post1";
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.DislikePost(id);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("User ID not found in token.");
        }

        // Tests for Delete (DELETE /api/posts/{id})
        [Fact]
        public async Task Delete_ValidPost_ReturnsNoContent()
        {
            // Arrange
            var id = "post1";
            var userId = "user1";
            var post = TestDataGenerator.Post(id: id, authorId: userId);
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync(post);
            _postServiceMock.Setup(s => s.DeletePostAsync(id)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            var id = "post1";
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("User ID not found in token.");
        }

        [Fact]
        public async Task Delete_PostNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = "nonexistent";
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync((Post)null);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("Post not found.");
        }

        [Fact]
        public async Task Delete_ForbiddenUser_ReturnsForbidden()
        {
            // Arrange
            var id = "post1";
            var userId = "user1";
            var post = TestDataGenerator.Post(id: id, authorId: "user2");
            _postServiceMock.Setup(s => s.GetPostByIdAsync(id)).ReturnsAsync(post);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }
    }
}
