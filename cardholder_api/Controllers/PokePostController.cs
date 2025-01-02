using cardholder_api.Models;
using cardholder_api.Repositories;
using cardholder_api.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cardholder_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PokemonPostController : ControllerBase
    {
        private readonly IPokemonPostRepository _repository;
        private readonly ICardHolderRepository _cardHolderRepository;
        private readonly UserManager<User> _userManager;

        public PokemonPostController(IPokemonPostRepository repository, UserManager<User> userManager,
            ICardHolderRepository cardHolderRepository)
        {
            _repository = repository;
            _userManager = userManager;
            _cardHolderRepository = cardHolderRepository;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PokemonPost>> CreatePost(PokemonPost post)
        {
            var user = await _userManager.GetUserAsync(User);

            // Verify user owns the card they're posting
            var userCard = await _cardHolderRepository.GetUserCardAsync(user.Id, post.CardId);
            if (userCard == null || userCard.Quantity < 1)
            {
                return BadRequest("You don't own this card");
            }

            post.PosterId = user.Id;
            post.CreatedAt = DateTime.UtcNow;
            post.Status = PostStatus.Active;

            await _repository.CreatePostAsync(post);
            return CreatedAtAction(nameof(GetPosts), new { id = post.Id }, post);
        }

        [HttpPost("{postId}/offers")]
        [Authorize]
        public async Task<ActionResult<TradeOffer>> CreateTradeOffer(int postId, TradeOffer offer)
        {
            var user = await _userManager.GetUserAsync(User);

            // Verify user owns all offered cards
            foreach (var offeredCard in offer.OfferedCards)
            {
                var userCard = await _cardHolderRepository.GetUserCardAsync(user.Id, offeredCard.CardId);
                if (userCard == null || userCard.Quantity < 1)
                {
                    return BadRequest($"You don't own the card {offeredCard.CardId}");
                }
            }

            offer.TraderId = user.Id;
            offer.PostId = postId;
            offer.OfferDate = DateTime.UtcNow;
            offer.Status = OfferStatus.Pending;

            await _repository.AddTradeOfferAsync(offer);
            return Ok(offer);
        }

        [HttpPut("offers/{offerId}/accept")]
        [Authorize]
        public async Task<IActionResult> AcceptTradeOffer(int offerId)
        {
            var user = await _userManager.GetUserAsync(User);
            var offer = await _repository.GetTradeOfferByIdAsync(offerId);

            if (offer == null)
                return NotFound();

            var post = await _repository.GetPostByIdAsync(offer.PostId);
            if (post.PosterId != user.Id)
                return Forbid();

            // Process the trade
            await _cardHolderRepository.ProcessTradeAsync(
                post.PosterId,
                offer.TraderId,
                post.CardId,
                1
            );

            // Process offered cards
            foreach (var offeredCard in offer.OfferedCards)
            {
                await _cardHolderRepository.ProcessTradeAsync(
                    offer.TraderId,
                    post.PosterId,
                    offeredCard.CardId,
                    1
                );
            }

            await _repository.UpdateTradeOfferStatusAsync(offerId, OfferStatus.Accepted);
            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PokemonPost>>> GetPosts()
        {
            return Ok(await _repository.GetAllPostsAsync());
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePostStatus(int id, PostStatus status)
        {
            await _repository.UpdatePostStatusAsync(id, status);
            return NoContent();
        }
    }
}
