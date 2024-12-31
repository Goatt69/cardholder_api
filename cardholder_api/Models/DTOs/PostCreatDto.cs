namespace cardholder_api.Models.DTOs;

public class PostCreateDto
{
    public string Content { get; set; }
    public string ImageUrl { get; set; }
    public bool IsPublic { get; set; } = true;
}
