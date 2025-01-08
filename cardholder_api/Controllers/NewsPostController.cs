using cardholder_api.Models;
using cardholder_api.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cardholder_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsPostController : ControllerBase
    {
        private readonly INewsPostRepository _newsPostRepository;

        public NewsPostController(INewsPostRepository newsPostRepository)
        {
            _newsPostRepository = newsPostRepository;
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
        public async Task<ActionResult<NewsPost>> CreateNewsPost(NewsPost newsPost)
        {
            var created = await _newsPostRepository.CreateNewsPostAsync(newsPost);
            return CreatedAtAction(nameof(GetNewsPost), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NewsPost>> UpdateNewsPost(int id, NewsPost newsPost)
        {
            if (id != newsPost.Id)
                return BadRequest();

            var updated = await _newsPostRepository.UpdateNewsPostAsync(newsPost);
            return Ok(updated);
        }
    }
}