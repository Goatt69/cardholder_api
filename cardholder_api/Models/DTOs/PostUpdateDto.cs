namespace cardholder_api.Models.DTOs;

public class PostUpdateDto
{
    public int Id { get; set; }
    public string Content { get; set; }
    public string ImageUrl { get; set; }
    public bool IsPublic { get; set; }
}
