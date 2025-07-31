using Amazon.DynamoDBv2.DataModel;

namespace HealthForumApi.Models
{
    [DynamoDBTable("Users")]
    public class User
    {
        [DynamoDBHashKey]
        public string? Id { get; set; }

        [DynamoDBGlobalSecondaryIndexHashKey("EmailIndex")]
        public string? Email { get; set; }

        public string? Username { get; set; }

        public string? PasswordHash { get; set; }

        public string? Bio { get; set; }
    }

}

