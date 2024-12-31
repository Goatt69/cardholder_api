using cardholder_api.Models;
using cardholder_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cardholder_api.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class CardHolderController : ControllerBase
    {
        private readonly ICardHolderRepository _cardHolderRepository;
        private readonly UserManager<User> _userManager;

        public CardHolderController(ICardHolderRepository cardHolderRepository, UserManager<User> userManager)
        {
            _cardHolderRepository = cardHolderRepository;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CardHolder>>> GetUserCards()
        {
            var user = await _userManager.GetUserAsync(User);
            var cards = await _cardHolderRepository.GetUserCardsAsync(user.Id);
            return Ok(cards);
        }

        [Authorize]
        [HttpPost("{cardId}")]
        public async Task<ActionResult<CardHolder>> AddCard(string cardId, [FromBody] int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            var card = await _cardHolderRepository.AddCardToUserAsync(user.Id, cardId, quantity);
            return Ok(card);
        }

        [Authorize]
        [HttpPut("{cardId}")]
        public async Task<IActionResult> UpdateCardQuantity(string cardId, [FromBody] int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            await _cardHolderRepository.UpdateCardQuantityAsync(user.Id, cardId, quantity);
            return NoContent();
        }
    }

}