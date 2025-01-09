using Microsoft.AspNetCore.Identity;

namespace cardholder_api.Models;

public class User : IdentityUser
{
    public string? Initials { get; set; }
    public string? TotpSecretKey { get; set; }
    public string? AvatarUrl { get; set; }
}