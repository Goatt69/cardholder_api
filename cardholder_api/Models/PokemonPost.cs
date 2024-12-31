using cardholder_api.Migrations;

namespace cardholder_api.Models;

public class PokemonPost
{
    public int Id { get; set; }
    public string CardId { get; set; }
    public string PosterId { get; set; }
    public string? BuyerId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public PostStatus Status { get; set; }
    
    public virtual User Poster { get; set; }
    public virtual User? Buyer { get; set; }
    public virtual pokemon_card Card { get; set; }
}

public enum PostStatus
{
    Active,
    Inactive,
    Disabled
}
