namespace HealthForumApi.Dtos
{
    public class CreateCommentDto
    {
        public string? PostId { get; set; }
        public string? Content { get; set; }
        public string? AuthorId { get; set; }
    }
}
