using cardholder_api.Models;

namespace cardholder_api.Repositories;

public interface IPokemonPostRepository
{
    Task<IEnumerable<PokemonPost>> GetPostsAsync();
    Task<PokemonPost> GetPostByIdAsync(int id);
    Task<PokemonPost> AddPostAsync(PokemonPost post);
    Task UpdatePostAsync(PokemonPost post);
    Task DeletePostAsync(int id);
    Task<PokemonPost> ChangePostStatusAsync(int id, PostStatus status);
}
