namespace HealthForumApi.Dtos
{
    public class CreatePostDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public List<string>? Tags { get; set; }
        public string? AuthorId { get; set; }
    }
}
