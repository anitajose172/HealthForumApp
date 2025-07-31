using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using FluentAssertions;
using HealthForumApi.Dtos;
using HealthForumApi.Models.HealthForumApi.Models;
using HealthForumApi.Services;
using HealthForumApi.Tests.TestData;
using Microsoft.Extensions.Logging;
using Moq;

namespace HealthForumApi.Tests.Services
{
    public class PostServiceTest
    {
        private readonly Mock<IDynamoDBContext> _dbContextMock;
        private readonly Mock<ILogger<PostService>> _loggerMock;
        private readonly PostService _service;

        public PostServiceTest()
        {
            _dbContextMock = new Mock<IDynamoDBContext>();
            _loggerMock = new Mock<ILogger<PostService>>();
            _service = new PostService(_dbContextMock.Object, _loggerMock.Object);
        }

        // Tests for CreatePostAsync
        [Fact]
        public async Task CreatePostAsync_ValidDto_ReturnsPost()
        {
            // Arrange
            var dto = new CreatePostDto
            {
                AuthorId = "user1",
                Title = "Test Post",
                Content = "Test Content",
                Tags = new List<string> { "tag1" }
            };
            _dbContextMock.Setup(db => db.SaveAsync(It.IsAny<Post>(), default)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreatePostAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.Title.Should().Be(dto.Title);
            result.Content.Should().Be(dto.Content);
            result.AuthorId.Should().Be(dto.AuthorId);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.Tags.Should().Contain(dto.Tags);
            result.Likes.Should().Be(0);
            result.Dislikes.Should().Be(0);
            _dbContextMock.Verify(db => db.SaveAsync(It.Is<Post>(p => p.Id == result.Id), default), Times.Once());
        }

        [Fact]
        public async Task CreatePostAsync_NullDto_ThrowsNullReferenceException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => _service.CreatePostAsync(null));
        }

        [Fact]
        public async Task CreatePostAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var dto = new CreatePostDto { AuthorId = "user1" };
            _dbContextMock.Setup(db => db.SaveAsync(It.IsAny<Post>(), default))
                .ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreatePostAsync(dto));
        }

        // Tests for GetPostByIdAsync
        [Fact]
        public async Task GetPostByIdAsync_PostExists_ReturnsPost()
        {
            // Arrange
            var id = "post1";
            var post = TestDataGenerator.Post(id: id);
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync(post);

            // Act
            var result = await _service.GetPostByIdAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(post);
        }

        [Fact]
        public async Task GetPostByIdAsync_PostNotFound_ReturnsNull()
        {
            // Arrange
            var id = "nonexistent";
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync((Post)null);

            // Act
            var result = await _service.GetPostByIdAsync(id);

            // Assert
            result.Should().BeNull();
        }

        
        [Fact]
        public async Task GetPostByIdAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var id = "post1";
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default))
                .ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetPostByIdAsync(id));
        }

                        
        [Fact]
        public async Task UpdatePostReactionAsync_PostNotFound_NoUpdate()
        {
            // Arrange
            var id = "nonexistent";
            var userId = "user1";
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync((Post)null);

            // Act
            await _service.UpdatePostReactionAsync(id, userId, "like");

            // Assert
            _dbContextMock.Verify(db => db.SaveAsync(It.IsAny<Post>(), default), Times.Never());
        }

        
        [Fact]
        public async Task UpdatePostReactionAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var id = "post1";
            var userId = "user1";
            var post = TestDataGenerator.Post(id: id);
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync(post);
            _dbContextMock.Setup(db => db.SaveAsync(post, default))
                .ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdatePostReactionAsync(id, userId, "like"));
        }

        // Tests for DeletePostAsync
        [Fact]
        public async Task DeletePostAsync_ValidPost_DeletesPost()
        {
            // Arrange
            var id = "post1";
            var post = TestDataGenerator.Post(id: id);
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync(post);
            _dbContextMock.Setup(db => db.DeleteAsync(post, default)).Returns(Task.CompletedTask);

            // Act
            await _service.DeletePostAsync(id);

            // Assert
            _dbContextMock.Verify(db => db.DeleteAsync(post, default), Times.Once());
        }

        [Fact]
        public async Task DeletePostAsync_PostNotFound_NoDelete()
        {
            // Arrange
            var id = "nonexistent";
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync((Post)null);

            // Act
            await _service.DeletePostAsync(id);

            // Assert
            _dbContextMock.Verify(db => db.DeleteAsync(It.IsAny<Post>(), default), Times.Never());
        }

        
        [Fact]
        public async Task DeletePostAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var id = "post1";
            var post = TestDataGenerator.Post(id: id);
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync(post);
            _dbContextMock.Setup(db => db.DeleteAsync(post, default))
                .ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeletePostAsync(id));
        }

        [Fact]
        public async Task UpdatePostAsync_NullId_ThrowsArgumentException()
        {
            // Arrange
            var dto = new UpdatePostDto { Id = null, Title = "Updated Title" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdatePostAsync(dto));
        }

        [Fact]
        public async Task UpdatePostAsync_PostNotFound_ReturnsNull()
        {
            // Arrange
            var id = "nonexistent";
            var dto = new UpdatePostDto { Id = id, Title = "Updated Title" };
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync((Post)null);

            // Act
            var result = await _service.UpdatePostAsync(dto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdatePostAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var id = "post1";
            var dto = new UpdatePostDto { Id = id, Title = "Updated Title" };
            var existingPost = TestDataGenerator.Post(id: id);
            _dbContextMock.Setup(db => db.LoadAsync<Post>(id, default)).ReturnsAsync(existingPost);
            _dbContextMock.Setup(db => db.SaveAsync(existingPost, default))
                .ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdatePostAsync(dto));
        }
    }
}
