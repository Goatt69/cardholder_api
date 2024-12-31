using cardholder_api.Models;
using Microsoft.EntityFrameworkCore;

namespace cardholder_api.Repositories;

public class CardHolderRepository : ICardHolderRepository
{
    private readonly ApplicationDbContext _context;

    public CardHolderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CardHolder>> GetUserCardsAsync(string userId)
    {
        return await _context.CardHolders
            .Include(ch => ch.Card)
            .Where(ch => ch.UserId == userId)
            .ToListAsync();
    }

    public async Task<CardHolder> GetUserCardAsync(string userId, string cardId)
    {
        return await _context.CardHolders
            .FirstOrDefaultAsync(ch => ch.UserId == userId && ch.CardId == cardId);
    }

    public async Task<CardHolder> AddCardToUserAsync(string userId, string cardId, int quantity = 1)
    {
        var existingCard = await GetUserCardAsync(userId, cardId);
        
        if (existingCard != null)
        {
            existingCard.Quantity += quantity;
            _context.CardHolders.Update(existingCard);
        }
        else
        {
            existingCard = new CardHolder
            {
                UserId = userId,
                CardId = cardId,
                Quantity = quantity
            };
            _context.CardHolders.Add(existingCard);
        }
        
        await _context.SaveChangesAsync();
        return existingCard;
    }

    public async Task UpdateCardQuantityAsync(string userId, string cardId, int quantity)
    {
        var cardHolder = await GetUserCardAsync(userId, cardId);
        if (cardHolder != null)
        {
            cardHolder.Quantity = quantity;
            _context.CardHolders.Update(cardHolder);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ProcessTradeAsync(string sellerId, string buyerId, string cardId, int quantity)
    {
        var sellerCard = await GetUserCardAsync(sellerId, cardId);
        var buyerCard = await GetUserCardAsync(buyerId, cardId);

        if (sellerCard != null && sellerCard.Quantity >= quantity)
        {
            sellerCard.Quantity -= quantity;
            _context.CardHolders.Update(sellerCard);

            if (buyerCard != null)
            {
                buyerCard.Quantity += quantity;
                _context.CardHolders.Update(buyerCard);
            }
            else
            {
                await AddCardToUserAsync(buyerId, cardId, quantity);
            }

            await _context.SaveChangesAsync();
        }
    }
}
