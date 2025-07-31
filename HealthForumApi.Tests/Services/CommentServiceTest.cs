using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using FluentAssertions;
using HealthForumApi.Models;
using HealthForumApi.Services;
using HealthForumApi.Tests.TestData;
using Moq;

namespace HealthForumApi.Tests.Services
{
    public class CommentServiceTest
    {
        private readonly Mock<IDynamoDBContext> _dbContextMock;
        private readonly CommentService _service;

        public CommentServiceTest()
        {
            _dbContextMock = new Mock<IDynamoDBContext>();
            _service = new CommentService(_dbContextMock.Object);
        }

        // Tests for CreateCommentAsync
        [Fact]
        public async Task CreateCommentAsync_ValidDto_ReturnsComment()
        {
            // Arrange
            var dto = TestDataGenerator.CreateCommentDto(postId: "post1", authorId: "user1");
            _dbContextMock.Setup(db => db.SaveAsync(It.IsAny<Comment>(), default)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateCommentAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeNullOrEmpty();
            result.PostId.Should().Be(dto.PostId);
            result.AuthorId.Should().Be(dto.AuthorId);
            result.Content.Should().Be(dto.Content);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _dbContextMock.Verify(db => db.SaveAsync(It.Is<Comment>(c => c.Id == result.Id), default), Times.Once());
        }

        [Fact]
        public async Task CreateCommentAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var dto = TestDataGenerator.CreateCommentDto(postId: "post1", authorId: "user1");
            _dbContextMock.Setup(db => db.SaveAsync(It.IsAny<Comment>(), default))
                .ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateCommentAsync(dto));
        }

        [Fact]
        public async Task CreateCommentAsync_NullDto_ThrowsNullReferenceException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => _service.CreateCommentAsync(null));
        }
                    

        [Fact]
        public async Task GetCommentsByPostIdAsync_NullPostId_ThrowsNullReferenceException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetCommentsByPostIdAsync(null));
        }

        // Tests for GetCommentByIdAsync
        [Fact]
        public async Task GetCommentByIdAsync_CommentExists_ReturnsComment()
        {
            // Arrange
            var commentId = "comment1";
            var comment = TestDataGenerator.Comment(id: commentId);
            _dbContextMock.Setup(db => db.LoadAsync<Comment>(commentId, default)).ReturnsAsync(comment);

            // Act
            var result = await _service.GetCommentByIdAsync(commentId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(comment);
        }

        [Fact]
        public async Task GetCommentByIdAsync_CommentNotFound_ReturnsNull()
        {
            // Arrange
            var commentId = "nonexistent";
            _dbContextMock.Setup(db => db.LoadAsync<Comment>(commentId, default)).ReturnsAsync((Comment)null);

            // Act
            var result = await _service.GetCommentByIdAsync(commentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteCommentAsync_NullCommentId_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.DeleteCommentAsync("post1", null));
        }

        // Tests for DeleteCommentAsync
        [Fact]
        public async Task DeleteCommentAsync_ValidComment_DeletesComment()
        {
            // Arrange
            var postId = "post1";
            var commentId = "comment1";
            var comment = TestDataGenerator.Comment(id: commentId, postId: postId);
            _dbContextMock.Setup(db => db.LoadAsync<Comment>(commentId, default)).ReturnsAsync(comment);
            _dbContextMock.Setup(db => db.DeleteAsync(comment, default)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteCommentAsync(postId, commentId);

            // Assert
            _dbContextMock.Verify(db => db.DeleteAsync(comment, default), Times.Once());
        }

        [Fact]
        public async Task DeleteCommentAsync_CommentNotFound_DoesNotDelete()
        {
            // Arrange
            var postId = "post1";
            var commentId = "nonexistent";
            _dbContextMock.Setup(db => db.LoadAsync<Comment>(commentId, default)).ReturnsAsync((Comment)null);

            // Act
            await _service.DeleteCommentAsync(postId, commentId);

            // Assert
            _dbContextMock.Verify(db => db.DeleteAsync(It.IsAny<Comment>(), default), Times.Never());
        }

        [Fact]
        public async Task DeleteCommentAsync_PostIdMismatch_DoesNotDelete()
        {
            // Arrange
            var postId = "post1";
            var commentId = "comment1";
            var comment = TestDataGenerator.Comment(id: commentId, postId: "differentPost");
            _dbContextMock.Setup(db => db.LoadAsync<Comment>(commentId, default)).ReturnsAsync(comment);

            // Act
            await _service.DeleteCommentAsync(postId, commentId);

            // Assert
            _dbContextMock.Verify(db => db.DeleteAsync(It.IsAny<Comment>(), default), Times.Never());
        }

        [Fact]
        public async Task DeleteCommentAsync_DynamoDBException_ThrowsException()
        {
            // Arrange
            var postId = "post1";
            var commentId = "comment1";
            var comment = TestDataGenerator.Comment(id: commentId, postId: postId);
            _dbContextMock.Setup(db => db.LoadAsync<Comment>(commentId, default)).ReturnsAsync(comment);
            _dbContextMock.Setup(db => db.DeleteAsync(comment, default)).ThrowsAsync(new Exception("DynamoDB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteCommentAsync(postId, commentId));
        }
    }
}