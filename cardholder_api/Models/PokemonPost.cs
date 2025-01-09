using cardholder_api.Migrations;

namespace cardholder_api.Models;

public class PokemonPost
{
    public int Id { get; set; }
    public string PosterId { get; set; }
    public string CardId { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public PostStatus Status { get; set; }
    public List<TradeOffer> TradeOffers { get; set; }
    public virtual User Poster { get; set; }
    public virtual pokemon_card Card { get; set; }
}

public enum PostStatus
{
    Active,
    Pending,
    Inactive,
    Disabled
}