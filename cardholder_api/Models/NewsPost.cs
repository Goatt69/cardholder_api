using System.ComponentModel.DataAnnotations;

namespace cardholder_api.Models;

public class NewsPost
{
    public int Id { get; set; }

    [Required] public string Title { get; set; }

    [Required] public string Content { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required] public string AdminId { get; set; }

    public User Admin { get; set; }

    public bool IsPublished { get; set; } = false;
}