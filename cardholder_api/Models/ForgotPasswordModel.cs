using System.ComponentModel.DataAnnotations;

namespace cardholder_api.Models
{
    public class ForgotPasswordModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
