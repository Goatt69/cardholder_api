using System.ComponentModel.DataAnnotations;

namespace cardholder_api.Models;

public class TotpVerificationModel
{
    [Required]
    public string Username { get; set; }
    [Required]
    public string TotpCode { get; set; }
}
