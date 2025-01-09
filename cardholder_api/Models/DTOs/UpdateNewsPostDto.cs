namespace cardholder_api.Models.DTOs;

public class UpdateNewsPostDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; }
}