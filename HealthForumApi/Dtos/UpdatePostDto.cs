namespace HealthForumApi.Dtos
{
    public class UpdatePostDto
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? AuthorId { get; set; }
    }
}
