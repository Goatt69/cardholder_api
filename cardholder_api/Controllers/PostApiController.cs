using System.Security.Claims;
using cardholder_api.Models.DTOs;
using cardholder_api.Respo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cardholder_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostApiController : ControllerBase
{
    private readonly IPostRepository _postRepository;

    public PostApiController(IPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts()
    {
        var posts = await _postRepository.GetPostsAsync();
        return Ok(posts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(int id)
    {
        var post = await _postRepository.GetPostByIdAsync(id);
        if (post == null)
            return NotFound();
        return Ok(post);
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] PostCreateDto createDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var post = new PostModel
        {
            Content = createDto.Content,
            ImageUrl = createDto.ImageUrl,
            IsPublic = createDto.IsPublic,
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            LikesCount = 0,
            CommentsCount = 0
        };

        var createdPost = await _postRepository.CreatePostAsync(post);
        return CreatedAtAction(nameof(GetPost), new { id = createdPost.Id }, createdPost);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] PostUpdateDto updateDto)
    {
        if (id != updateDto.Id)
            return BadRequest();
        
        var post = await _postRepository.GetPostByIdAsync(id);
        if (post == null)
            return NotFound();
        
        post.Content = updateDto.Content;
        post.ImageUrl = updateDto.ImageUrl;
        post.IsPublic = updateDto.IsPublic;
    
        await _postRepository.UpdatePostAsync(post);
        return NoContent();
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        await _postRepository.DeletePostAsync(id);
        return NoContent();
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPosts(string userId)
    {
        var posts = await _postRepository.GetUserPostsAsync(userId);
        return Ok(posts);
    }
}