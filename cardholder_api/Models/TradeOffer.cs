using cardholder_api.Migrations;

namespace cardholder_api.Models;

public class TradeOffer
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string TraderId { get; set; }
    public DateTime OfferDate { get; set; }
    public OfferStatus Status { get; set; }
    public List<OfferedCard> OfferedCards { get; set; }
    public virtual User Trader { get; set; }
    public virtual PokemonPost Post { get; set; }
}

public class OfferedCard
{
    public int Id { get; set; }
    public int TradeOfferId { get; set; }
    public string CardId { get; set; }
    public virtual TradeOffer TradeOffer { get; set; }
    public virtual pokemon_card Card { get; set; }
}

public enum OfferStatus
{
    Pending,
    Accepted,
    Rejected
}
