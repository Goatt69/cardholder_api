using cardholder_api.Models;

namespace cardholder_api.Repositories.IRepositories
{
    public interface INewsPostRepository
    {
        Task<IEnumerable<NewsPost>> GetAllNewsPostsAsync();
        Task<NewsPost> GetNewsPostByIdAsync(int id);
        Task<NewsPost> CreateNewsPostAsync(NewsPost newsPost);
        Task<NewsPost> UpdateNewsPostAsync(NewsPost newsPost);
    }
}