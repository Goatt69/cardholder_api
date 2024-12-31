using cardholder_api.Models;
using cardholder_api.Respo;
using Microsoft.EntityFrameworkCore;

namespace cardholder_api.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly ApplicationDbContext _context;
        
        public PostRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PostModel>> GetPostsAsync()
        {
            return await _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<PostModel> GetPostByIdAsync(int id)
        {
            return await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PostModel> CreatePostAsync(PostModel postModel)
        {
            postModel.CreatedAt = DateTime.UtcNow;
            await _context.Posts.AddAsync(postModel);
            await _context.SaveChangesAsync();
            
            // Reload the post with user data
            await _context.Entry(postModel)
                .Reference(p => p.User)
                .LoadAsync();
                
            return postModel;
        }

        public async Task UpdatePostAsync(PostModel postModel)
        {
            var existingPost = await _context.Posts.FindAsync(postModel.Id);
            if (existingPost != null)
            {
                existingPost.Content = postModel.Content;
                existingPost.ImageUrl = postModel.ImageUrl;
                existingPost.IsPublic = postModel.IsPublic;
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeletePostAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<PostModel>> GetUserPostsAsync(string userId)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
