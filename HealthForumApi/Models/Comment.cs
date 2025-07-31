using Amazon.DynamoDBv2.DataModel;

namespace HealthForumApi.Models
{
    [DynamoDBTable("Comments")]
    public class Comment
    {
        [DynamoDBHashKey]
        public string? PostId { get; set; }
        [DynamoDBRangeKey]
        public string? Id { get; set; }
        public string? AuthorId { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
