using Amazon.DynamoDBv2.DataModel;

namespace HealthForumApi.Models
{
    using Amazon.DynamoDBv2.DataModel;

    namespace HealthForumApi.Models
    {
        [DynamoDBTable("Posts")]
        public class Post
        {
            [DynamoDBHashKey]
            public string? Id { get; set; }
            [DynamoDBProperty]
            public string? Title { get; set; }
            [DynamoDBProperty]
            public string? Content { get; set; }
            [DynamoDBProperty]
            public string? AuthorId { get; set; }
            [DynamoDBProperty]
            public DateTime CreatedAt { get; set; }
            [DynamoDBProperty]
            public List<string?>? Tags { get; set; }
            [DynamoDBProperty]
            public int Likes { get; set; } = 0;
            [DynamoDBProperty]
            public int Dislikes { get; set; } = 0;
        }
    }
}
