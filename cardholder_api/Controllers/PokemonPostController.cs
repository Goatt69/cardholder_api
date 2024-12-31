using cardholder_api.Models;
using cardholder_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cardholder_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PokemonPostController : ControllerBase
    {
        private readonly IPokemonPostRepository _postRepository;
        private readonly UserManager<User> _userManager;
        private readonly ICardHolderRepository _cardHolderRepository;
        
        public PokemonPostController(IPokemonPostRepository postRepository, UserManager<User> userManager,ICardHolderRepository cardHolderRepository)
        {
            _postRepository = postRepository;
            _userManager = userManager;
            _cardHolderRepository = cardHolderRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PokemonPost>>> GetPosts()
        {
            var posts = await _postRepository.GetPostsAsync();
            return Ok(posts);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PokemonPost>> CreatePost(PokemonPost post)
        {
            var user = await _userManager.GetUserAsync(User);
            post.PosterId = user.Id;
            post.CreatedAt = DateTime.UtcNow;
            post.Status = PostStatus.Active;

            var createdPost = await _postRepository.AddPostAsync(post);
            return CreatedAtAction(nameof(GetPosts), new { id = createdPost.Id }, createdPost);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, PokemonPost post)
        {
            var user = await _userManager.GetUserAsync(User);
            var existingPost = await _postRepository.GetPostByIdAsync(id);

            if (existingPost == null)
                return NotFound();

            if (existingPost.PosterId != user.Id && !User.IsInRole("Admin"))
                return Forbid();

            existingPost.Title = post.Title;
            existingPost.Description = post.Description;
            existingPost.Price = post.Price;

            await _postRepository.UpdatePostAsync(existingPost);
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var post = await _postRepository.GetPostByIdAsync(id);

            if (post == null)
                return NotFound();

            if (post.PosterId != user.Id && !User.IsInRole("Admin"))
                return Forbid();

            await _postRepository.DeletePostAsync(id);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangePostStatus(int id, PostStatus newStatus)
        {
            var updatedPost = await _postRepository.ChangePostStatusAsync(id, newStatus);
            if (updatedPost == null)
                return NotFound();

            return Ok(new { Message = $"Post status updated to {newStatus}", Post = updatedPost });
        }
        [Authorize]
        [HttpPost("{id}/process")]
        public async Task<IActionResult> ProcessPost(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var post = await _postRepository.GetPostByIdAsync(id);

            if (post == null)
                return NotFound();

            if (post.Status != PostStatus.Active)
                return BadRequest("Post is not active");

            await _cardHolderRepository.ProcessTradeAsync(post.PosterId, user.Id, post.CardId, 1);
            post.Status = PostStatus.Inactive;
            post.BuyerId = user.Id;
            await _postRepository.UpdatePostAsync(post);

            return Ok(new { Message = "Trade processed successfully" });
        }

    }

}

