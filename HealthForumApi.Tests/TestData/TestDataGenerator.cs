using HealthForumApi.Dtos;
using HealthForumApi.Models.HealthForumApi.Models;
using HealthForumApi.Models;

namespace HealthForumApi.Tests.TestData
{
    internal class TestDataGenerator
    {
        public static CreatePostDto CreatePostDto(string authorId = "user1")
        {
            return new CreatePostDto
            {
                Title = "Test Post",
                Content = "This is a test post.",
                Tags = new[] { "test", "sample" }.ToList(),
                AuthorId = authorId
            };
        }

        public static Post Post(string id = "post1", string authorId = "user1") => new Post
        {
            Id = id,
            Title = "Test Post",
            Content = "This is a test post.",
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow,
            Tags = new List<string> { "test", "sample" },
            Likes = 0,
            Dislikes = 0
        };

        public static CreateCommentDto CreateCommentDto(string postId = "post1", string authorId = "user1") => new CreateCommentDto
        {
            Content = "This is a test comment.",
            PostId = postId,
            AuthorId = authorId
        };

        public static Comment Comment(string id = "comment1", string postId = "post1", string authorId = "user1") => new Comment
        {
            Id = id,
            Content = "This is a test comment.",
            PostId = postId,
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow
        };

        public static RegisterDto RegisterDto() => new RegisterDto
        {
            Email = "test@example.com",
            Password = "Password123!",
            Username = "testuser"
        };

        public static User User(string id = "user1", string email = null, string v = null, string v1 = null) => new User
        {
            Id = "112",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Username = "testuser",
            Bio = ""
        };
    }
}
