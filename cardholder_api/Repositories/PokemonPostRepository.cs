using cardholder_api.Models;
using cardholder_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace cardholder_api.Repositories;

public class PokemonPostRepository : IPokemonPostRepository
{
    private readonly ApplicationDbContext _context;

    public PokemonPostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PokemonPost>> GetAllPostsAsync()
    {
        return await _context.PokemonPosts
            .Include(p => p.Poster)
            .Include(p => p.Card)
            .Include(p => p.TradeOffers)
            .ThenInclude(t => t.OfferedCards)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<PokemonPost> GetPostByIdAsync(int id)
    {
        return await _context.PokemonPosts
            .Include(p => p.Poster)
            .Include(p => p.Card)
            .Include(p => p.TradeOffers)
            .ThenInclude(t => t.OfferedCards)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<PokemonPost>> GetPostsByUserIdAsync(string userId)
    {
        return await _context.PokemonPosts
            .Include(p => p.TradeOffers)
            .ThenInclude(t => t.OfferedCards)
            .Where(p => p.PosterId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task CreatePostAsync(PokemonPost post)
    {
        _context.PokemonPosts.Add(post);
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePostAsync(PokemonPost post)
    {
        _context.Entry(post).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task<TradeOffer> GetTradeOfferByIdAsync(int offerId)
    {
        return await _context.TradeOffers
            .Include(t => t.OfferedCards)
            .FirstOrDefaultAsync(t => t.Id == offerId);
    }

    public async Task UpdatePostStatusAsync(int postId, PostStatus status)
    {
        var post = await _context.PokemonPosts.FindAsync(postId);
        if (post != null)
        {
            post.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddTradeOfferAsync(TradeOffer offer)
    {
        _context.TradeOffers.Add(offer);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTradeOfferStatusAsync(int offerId, OfferStatus status)
    {
        var offer = await _context.TradeOffers.FindAsync(offerId);
        if (offer != null)
        {
            offer.Status = status;
            if (status == OfferStatus.Accepted)
            {
                var post = await _context.PokemonPosts.FindAsync(offer.PostId);
                if (post != null) post.Status = PostStatus.Inactive;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<TradeOffer>> GetTradeOffersByPostIdAsync(int postId)
    {
        return await _context.TradeOffers
            .Include(t => t.Trader)
            .Include(t => t.OfferedCards)
            .ThenInclude(oc => oc.Card) // Include full card data
            .Where(t => t.PostId == postId)
            .OrderByDescending(t => t.OfferDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TradeOffer>> GetTradeOffersByUserIdAsync(string userId)
    {
        return await _context.TradeOffers
            .Include(t => t.Post)
            .Include(t => t.OfferedCards)
            .Where(t => t.TraderId == userId)
            .OrderByDescending(t => t.OfferDate)
            .ToListAsync();
    }
}