using System.ComponentModel.DataAnnotations;

namespace cardholder_api.Models;

public class ChangePasswordModel
{
    [Required]
    public string OldPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }
    
    [Required, Compare("Password")]
    public string RetypeNewPassword { get; set; }
}
