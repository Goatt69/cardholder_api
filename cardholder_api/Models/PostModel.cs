using cardholder_api.Models;

namespace cardholder_api
{
    public class PostModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsPublic { get; set; } = true;
    }
}
