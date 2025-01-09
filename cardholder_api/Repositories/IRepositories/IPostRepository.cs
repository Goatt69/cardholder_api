namespace cardholder_api.Respo;

public interface IPostRepository
{
    Task<IEnumerable<PostModel>> GetPostsAsync();
    Task<PostModel> GetPostByIdAsync(int id);
    Task<PostModel> CreatePostAsync(PostModel postModel);
    Task UpdatePostAsync(PostModel postModel);
    Task DeletePostAsync(int id);
    Task<IEnumerable<PostModel>> GetUserPostsAsync(string userId);
}