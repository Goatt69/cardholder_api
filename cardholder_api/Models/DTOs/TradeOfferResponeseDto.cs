using cardholder_api.Migrations;

namespace cardholder_api.Models.DTOs;

public class TradeOfferResponseDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string TraderId { get; set; }
    public DateTime OfferDate { get; set; }
    public OfferStatus Status { get; set; }
    public List<OfferedCardDto> OfferedCards { get; set; }
}

public class OfferedCardDto
{
    public int Id { get; set; }
    public string CardId { get; set; }
    public virtual pokemon_card Card { get; set; }
}