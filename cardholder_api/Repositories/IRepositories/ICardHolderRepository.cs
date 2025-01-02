using cardholder_api.Models;

namespace cardholder_api.Repositories;

public interface ICardHolderRepository
{
    Task<IEnumerable<CardHolder>> GetUserCardsAsync(string userId);
    Task<CardHolder> GetUserCardAsync(string userId, string cardId);
    Task<CardHolder> AddCardToUserAsync(string userId, string cardId, int quantity = 1);
    Task UpdateCardQuantityAsync(string userId, string cardId, int quantity);
    Task ProcessTradeAsync(string fromUserId, string toUserId, string cardId, int quantity);
}
