using cardholder_api.Models;
using cardholder_api.Models.DTOs;
using cardholder_api.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cardholder_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsPostController : ControllerBase
    {
        private readonly INewsPostRepository _newsPostRepository;
        private readonly UserManager<User> _userManager;

        public NewsPostController(INewsPostRepository newsPostRepository, UserManager<User> userManager)
        {
            _newsPostRepository = newsPostRepository;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NewsPost>>> GetNewsPosts()
        {
            var posts = await _newsPostRepository.GetAllNewsPostsAsync();
            return Ok(posts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NewsPost>> GetNewsPost(int id)
        {
            var post = await _newsPostRepository.GetNewsPostByIdAsync(id);
            if (post == null)
                return NotFound();
            return Ok(post);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NewsPost>> CreateNewsPost(CreateNewsPostDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            var newsPost = new NewsPost
            {
                Title = dto.Title,
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                IsPublished = dto.IsPublished,
                AdminId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _newsPostRepository.CreateNewsPostAsync(newsPost);
            return CreatedAtAction(nameof(GetNewsPost), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NewsPost>> UpdateNewsPost(int id, UpdateNewsPostDto dto)
        {
            var newsPost = new NewsPost
            {
                Title = dto.Title,
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                IsPublished = dto.IsPublished
            };

            var updated = await _newsPostRepository.UpdateNewsPostAsync(id, newsPost);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }


    }
}