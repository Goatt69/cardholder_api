using cardholder_api.Models;
using Microsoft.EntityFrameworkCore;

namespace cardholder_api.Repositories;

public class PokemonPostRepository : IPokemonPostRepository
{
    private readonly ApplicationDbContext _context;

    public PokemonPostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PokemonPost>> GetPostsAsync()
    {
        return await _context.PokemonPosts
            .Include(p => p.Poster)
            .Include(p => p.Card)
            .Where(p => p.Status != PostStatus.Disabled)
            .ToListAsync();
    }

    public async Task<PokemonPost> GetPostByIdAsync(int id)
    {
        return await _context.PokemonPosts
            .Include(p => p.Poster)
            .Include(p => p.Card)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PokemonPost> AddPostAsync(PokemonPost post)
    {
        _context.PokemonPosts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task UpdatePostAsync(PokemonPost post)
    {
        _context.Entry(post).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await _context.PokemonPosts.FindAsync(id);
        if (post != null)
        {
            _context.PokemonPosts.Remove(post);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PokemonPost> ChangePostStatusAsync(int id, PostStatus status)
    {
        var post = await _context.PokemonPosts.FindAsync(id);
        if (post != null)
        {
            post.Status = status;
            await _context.SaveChangesAsync();
        }
        return post;
    }
}
