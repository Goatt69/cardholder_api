using cardholder_api.Migrations;

namespace cardholder_api.Models;

public class CardHolder
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string CardId { get; set; }
    public int Quantity { get; set; }

    public virtual User User { get; set; }
    public virtual pokemon_card Card { get; set; }
}