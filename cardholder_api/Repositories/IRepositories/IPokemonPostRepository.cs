using cardholder_api.Models;

namespace cardholder_api.Repositories.IRepositories;

public interface IPokemonPostRepository
{
    Task<IEnumerable<PokemonPost>> GetAllPostsAsync();
    Task<PokemonPost> GetPostByIdAsync(int id);
    Task<IEnumerable<PokemonPost>> GetPostsByUserIdAsync(string userId);
    Task CreatePostAsync(PokemonPost post);
    Task<TradeOffer> GetTradeOfferByIdAsync(int id);
    Task<IEnumerable<TradeOffer>> GetTradeOffersByPostIdAsync(int postId);
    Task UpdatePostAsync(PokemonPost post);
    Task UpdatePostStatusAsync(int postId, PostStatus status);
    Task AddTradeOfferAsync(TradeOffer offer);
    Task UpdateTradeOfferStatusAsync(int offerId, OfferStatus status);
}