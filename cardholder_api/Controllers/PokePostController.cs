using System.Security.Claims;
using cardholder_api.Models;
using cardholder_api.Models.DTOs;
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
        public async Task<ActionResult<PokemonPost>> CreatePost(PokePostCreateDto dto)
        {
            var username = User.Identity.Name;
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound();

            // Verify user owns the card they're posting
            var userCard = await _cardHolderRepository.GetUserCardAsync(user.Id, dto.CardId);
            if (userCard == null || userCard.Quantity < 1)
            {
                return BadRequest("You don't own this card");
            }

            var post = new PokemonPost
            {
                PosterId = user.Id,
                CardId = dto.CardId,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                Status = PostStatus.Active
            };

            await _repository.CreatePostAsync(post);
            return CreatedAtAction(nameof(GetPosts), new { id = post.Id }, post);
        }
        
        [HttpPost("{postId}/offers")]
        [Authorize]
        public async Task<ActionResult<TradeOffer>> CreateTradeOffer(int postId, TradeOfferDto dto)
        {
            var username = User.Identity.Name;
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound();
    
            // First verify the post exists
            var post = await _repository.GetPostByIdAsync(postId);
            if (post == null)
                return NotFound("Post not found");
            
            var offer = new TradeOffer
            {
                PostId = postId,
                TraderId = user.Id,
                OfferDate = DateTime.UtcNow,
                Status = OfferStatus.Pending,
                OfferedCards = dto.OfferedCardIds.Select(cardId => new OfferedCard
                {
                    CardId = cardId
                }).ToList()
            };

            // Verify user owns all offered cards
            foreach (var offeredCard in offer.OfferedCards)
            {
                var userCard = await _cardHolderRepository.GetUserCardAsync(user.Id, offeredCard.CardId);
                if (userCard == null || userCard.Quantity < 1)
                {
                    return BadRequest($"You don't own the card {offeredCard.CardId}");
                }
            }

            await _repository.AddTradeOfferAsync(offer);

            var response = new TradeOfferResponseDto()
            {
                Id = offer.Id,
                PostId = offer.PostId,
                TraderId = offer.TraderId,
                OfferDate = offer.OfferDate,
                Status = offer.Status,
                OfferedCards = offer.OfferedCards.Select(oc => new OfferedCardDto
                {
                    Id = oc.Id,
                    CardId = oc.CardId
                }).ToList()
            };
            return Ok(response);
        }

        [HttpPut("offers/{offerId}/accept")]
        [Authorize]
        public async Task<IActionResult> AcceptTradeOffer(int offerId)
        {
            var username = User.Identity.Name;
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound();
            
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
        
        [HttpGet("{postId}/offers")]
        public async Task<ActionResult<IEnumerable<TradeOfferResponseDto>>> GetOffersForPost(int postId)
        {
            var offers = await _repository.GetTradeOffersByPostIdAsync(postId);
    
            var response = offers.Select(offer => new TradeOfferResponseDto
            {
                Id = offer.Id,
                PostId = offer.PostId,
                TraderId = offer.TraderId,
                OfferDate = offer.OfferDate,
                Status = offer.Status,
                OfferedCards = offer.OfferedCards.Select(oc => new OfferedCardDto
                {
                    Id = oc.Id,
                    CardId = oc.CardId,
                    Card = oc.Card // Include full card details
                }).ToList()
            });

            return Ok(response);
        }
    }
}
