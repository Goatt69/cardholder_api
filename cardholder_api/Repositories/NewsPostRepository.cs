using cardholder_api.Models;
using cardholder_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace cardholder_api.Repositories;

public class NewsPostRepository : INewsPostRepository
{
    private readonly ApplicationDbContext _context;

    public NewsPostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NewsPost>> GetAllNewsPostsAsync()
    {
        return await _context.NewsPosts
            .Include(n => n.Admin)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<NewsPost> GetNewsPostByIdAsync(int id)
    {
        return await _context.NewsPosts
            .Include(n => n.Admin)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<NewsPost> CreateNewsPostAsync(NewsPost newsPost)
    {
        _context.NewsPosts.Add(newsPost);
        await _context.SaveChangesAsync();
        return newsPost;
    }

    public async Task<NewsPost> UpdateNewsPostAsync(int id, NewsPost newsPost)
    {
        var existing = await _context.NewsPosts.FindAsync(id);
        if (existing != null)
        {
            existing.Title = newsPost.Title;
            existing.Content = newsPost.Content;
            existing.ImageUrl = newsPost.ImageUrl;
            existing.IsPublished = newsPost.IsPublished;
            await _context.SaveChangesAsync();
        }

        return existing;
    }
}