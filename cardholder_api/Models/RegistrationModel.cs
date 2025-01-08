﻿using System.ComponentModel.DataAnnotations;

namespace cardholder_api.Models
{
    public class RegistrationModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
    
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

}
