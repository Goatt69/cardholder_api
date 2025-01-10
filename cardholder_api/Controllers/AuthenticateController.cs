using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using cardholder_api.Models;
using cardholder_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OtpNet;

namespace cardholder_api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticateController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<User> _userManager;

    public AuthenticateController(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        EmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistrationModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userExists = await _userManager.FindByNameAsync(model.Username);
        if (userExists != null)
            return StatusCode(StatusCodes.Status400BadRequest,
                new { Status = false, Message = "User already exists" });

        var emailExists = await _userManager.FindByEmailAsync(model.Email);
        if (emailExists != null)
            return StatusCode(StatusCodes.Status400BadRequest,
                new { Status = false, Message = "Email already registered" });

        var user = new User
        {
            UserName = model.Username,
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Status = false, Message = "User creation failed" });

        // Assign default User role
        await _userManager.AddToRoleAsync(user, "User");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = Url.Action("ConfirmEmail", "Authenticate",
            new { userId = user.Id, token }, Request.Scheme);

        await _emailService.SendEmailAsync(user.Email, "Confirm your email",
            $"Please confirm your email by clicking this link: {confirmationLink}");

        return Ok(new { Status = true, Message = "User created successfully" });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
            return Ok(new { Status = true, Message = "Email confirmed successfully" });

        return BadRequest(new { Status = false, Message = "Email confirmation failed" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Ok(new { Status = true, Message = "If the email exists, password reset instructions will be sent" });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var confirmationLink = Url.Action("ResetPassword", "Authenticate",
            new { email = user.Email, token }, Request.Scheme);

        var emailBody = $@"
        <h2>Password Reset Request</h2>
        <p>Click the link below to confirm your password reset:</p>
        <p><a href='{confirmationLink}'>Reset Password</a></p>
        <p>If you didn't request this, please ignore this email.</p>";

        await _emailService.SendEmailAsync(
            user.Email,
            "Password Reset Confirmation",
            emailBody
        );

        return Ok(new { Status = true, Message = "Password reset confirmation has been sent to your email" });
    }

    [HttpGet("reset-password")]
    public async Task<IActionResult> ResetPassword(string email, string token)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound();

        var newPassword = GenerateRandomPassword();
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            var emailBody = $@"
            <h2>New Password</h2>
            <p>Your password has been reset successfully. Here is your new password:</p>
            <p><strong>{newPassword}</strong></p>
            <p>Please change this password after logging in.</p>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Your New Password",
                emailBody
            );

            return Ok(new { Status = true, Message = "New password has been sent to your email" });
        }

        return BadRequest(new { Status = false, Message = "Password reset failed", Errors = result.Errors });
    }


    private string GenerateRandomPassword()
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()";

        var random = new Random();
        var password = new StringBuilder();

        // Ensure at least one of each required character type
        password.Append(upperCase[random.Next(upperCase.Length)]);
        password.Append(lowerCase[random.Next(lowerCase.Length)]);
        password.Append(digits[random.Next(digits.Length)]);
        password.Append(special[random.Next(special.Length)]);

        // Fill the rest to reach desired length
        while (password.Length < 12)
        {
            var allChars = upperCase + lowerCase + digits + special;
            password.Append(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the final password
        return new string(password.ToString().ToCharArray().OrderBy(x => random.Next()).ToArray());
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var normalizedEmail = model.Email.ToUpperInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized(new { Status = false, Message = "Invalid username or password" });

        if (user.TwoFactorEnabled)
            return Ok(new
            {
                Status = true,
                RequiresTwoFactor = true,
                Message = "2FA is enabled, please provide TOTP code",
                twoFactorEnabled = user.TwoFactorEnabled,
                SecretKey = string.IsNullOrEmpty(user.TotpSecretKey) ? GenerateSecretKey() : user.TotpSecretKey
            });

        if (!user.EmailConfirmed) return BadRequest(new { Status = false, Message = "Email not confirmed" });

        var userRoles = await _userManager.GetRolesAsync(user);
        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var userRole in userRoles)
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));

        var token = GenerateToken(authClaims);
        var rolesString = string.Join(", ", userRoles);
        return Ok(new
        {
            Status = true,
            RequiresTwoFactor = false,
            Message = "Logged in successfully",
            Token = token,
            twoFactorEnabled = user.TwoFactorEnabled,
            Roles = rolesString
        });
    }

    [HttpPost("login-with-totp")]
    public async Task<IActionResult> LoginWithTotp([FromBody] LoginModel model, [FromQuery] string totpCode)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized(new { Status = false, Message = "Invalid email or password" });

        if (string.IsNullOrEmpty(user.TotpSecretKey))
            return BadRequest("TOTP not set up for this user");

        var totp = new Totp(Base32Encoding.ToBytes(user.TotpSecretKey)
        );
        var unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

        var timeWindow = unixTimestamp / 30;

        // Generate valid codes for the time window
        var currentTotp = totp.ComputeTotp(DateTime.UnixEpoch.AddSeconds(timeWindow * 30));
        // Check if provided code matches any valid code
        var isValid = totpCode == currentTotp;

        if (!isValid)
            return BadRequest("Invalid TOTP code");

        var userRoles = await _userManager.GetRolesAsync(user);
        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var userRole in userRoles) authClaims.Add(new Claim(ClaimTypes.Role, userRole));

        var token = GenerateToken(authClaims);
        return Ok(new { Status = true, Message = "Logged in successfully", Token = token });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Không cần làm gì ở phía server vì JWT là stateless
        return Ok(new { Status = true, Message = "Logged out successfully" });
    }

    private string GenerateToken(IEnumerable<Claim> claims)
    {
        var jwtSettings = _configuration.GetSection("JWTKey");
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["TokenExpiryTimeInHour"])),
            Issuer = jwtSettings["ValidIssuer"],
            Audience = jwtSettings["ValidAudience"],
            SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateSecretKey()
    {
        var bytes = new byte[32];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(bytes);
        }

        return Base32Encoding.ToString(bytes);
    }

    [Authorize]
    [HttpGet("setup-totp")]
    public async Task<IActionResult> SetupTotp()
    {
        var username = User.Identity.Name;
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return NotFound();

        if (string.IsNullOrEmpty(user.TotpSecretKey))
        {
            user.TotpSecretKey = GenerateSecretKey();
            user.TwoFactorEnabled = true;
            await _userManager.UpdateAsync(user);

            var emailBody = $@"
                    <h2>Two-Factor Authentication Setup</h2>
                    <p>Your TOTP Secret Key is:</p>
                    <p><strong>{user.TotpSecretKey}</strong></p>
                    <p>Please use this key to set up your authenticator app.</p>
                    <p>Keep this key secure and do not share it with anyone.</p>";

            await _emailService.SendEmailAsync(
                user.Email,
                "TOTP Setup Information",
                emailBody
            );
        }

        return Ok(new
        {
            status = true,
            Message = "TOTP setup initiated. Check your email for the secret key.",
            SecretKey = user.TotpSecretKey
        });
    }

    [HttpPost("verify-totp")]
    public async Task<IActionResult> VerifyTotp([FromBody] TotpVerificationModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null || string.IsNullOrEmpty(user.TotpSecretKey))
            return BadRequest("Invalid user or TOTP not set up");

        var totp = new Totp(Base32Encoding.ToBytes(user.TotpSecretKey));

        // Get Unix timestamp for the current time window
        var unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

        var timeWindow = unixTimestamp / 30;

        // Generate valid codes for the time window
        var currentTotp = totp.ComputeTotp(DateTime.UnixEpoch.AddSeconds(timeWindow * 30));
        // Check if provided code matches any valid code
        var isValid = model.TotpCode == currentTotp;

        // For debugging
        Console.WriteLine($"Expected TOTP: {currentTotp}, Received: {model.TotpCode}");

        if (!isValid)
            return BadRequest($"Invalid TOTP code. Expected: {currentTotp}");

        return Ok(new { Status = true, Message = "TOTP verified successfully" });
    }

    [Authorize]
    [HttpGet("user-details")]
    public async Task<IActionResult> GetUserDetails()
    {
        var username = User.Identity.Name;
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return NotFound(new { Status = false, Message = "User not found" });

        return Ok(new
        {
            Status = true,
            UserDetails = new
            {
                user.Id,
                Username = user.UserName,
                user.Email,
                user.AvatarUrl
            }
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (model.NewPassword != model.RetypeNewPassword)
            return BadRequest(new { Status = false, Message = "New password and retype password do not match" });

        var username = User.Identity.Name;
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
            return NotFound(new { Status = false, Message = "User not found" });

        var isOldPasswordValid = await _userManager.CheckPasswordAsync(user, model.OldPassword);
        if (!isOldPasswordValid)
            return BadRequest(new { Status = false, Message = "Old password is incorrect" });

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

        if (result.Succeeded)
            return Ok(new { Status = true, Message = "Password changed successfully" });

        return BadRequest(new { Status = false, Message = "Failed to change password", result.Errors });
    }

    [Authorize]
    [HttpPut("update-avatar")]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarModel model)
    {
        var username = User.Identity.Name;
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
            return NotFound(new { Status = false, Message = "User not found" });

        user.AvatarUrl = model.AvatarUrl;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
            return Ok(new { Status = true, Message = "Avatar updated successfully", user.AvatarUrl });

        return StatusCode(StatusCodes.Status500InternalServerError,
            new { Status = false, Message = "Avatar update failed" });
    }
}