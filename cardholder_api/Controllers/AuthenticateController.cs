using cardholder_api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using cardholder_api.Services;
using Microsoft.AspNetCore.Authorization;
using OtpNet;

namespace cardholder_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

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
                EmailConfirmed = false,
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Status = false, Message = "User creation failed" });

            // Assign default User role
            await _userManager.AddToRoleAsync(user, "User");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Authenticate", 
                new { userId = user.Id, token = token }, Request.Scheme);
            
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

            var newPassword = GenerateRandomPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                var emailBody = $@"
            <h2>Password Reset</h2>
            <p>Your password has been reset. Here is your new password:</p>
            <p><strong>{newPassword}</strong></p>
            <p>Please change this password after logging in.</p>";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Password Reset Confirmation",
                    emailBody
                );

                return Ok(new { Status = true, Message = "New password has been sent to your email" });
            }

            return BadRequest(new { Status = false, Message = "Password reset failed", Errors = result.Errors });
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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
            {
                return Ok(new
                {
                    Status = true,
                    RequiresTwoFactor = true,
                    Message = "2FA is enabled, please provide TOTP code",
                    twoFactorEnabled = user.TwoFactorEnabled,
                    SecretKey = string.IsNullOrEmpty(user.TotpSecretKey) ? GenerateSecretKey() : user.TotpSecretKey
                });
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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

            var totp = new OtpNet.Totp(Base32Encoding.ToBytes(user.TotpSecretKey),
                step: 30,
                totpSize: 6,
                mode: OtpHashMode.Sha1
            );
            long unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            long timeWindow = unixTimestamp / 30;

            // Generate valid codes for the time window
            var currentTotp = totp.ComputeTotp(DateTime.UnixEpoch.AddSeconds(timeWindow * 30));
            // Check if provided code matches any valid code
            bool isValid = totpCode == currentTotp;

            if (!isValid)
                return BadRequest("Invalid TOTP code");

            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

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
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
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
            }

            return Ok(new { SecretKey = user.TotpSecretKey });
        }

        [HttpPost("verify-totp")]
        public async Task<IActionResult> VerifyTotp([FromBody] TotpVerificationModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || string.IsNullOrEmpty(user.TotpSecretKey))
                return BadRequest("Invalid user or TOTP not set up");

            var totp = new OtpNet.Totp(Base32Encoding.ToBytes(user.TotpSecretKey),
                step: 30,
                mode: OtpHashMode.Sha1,
                totpSize: 6);

            // Get Unix timestamp for the current time window
            long unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            long timeWindow = unixTimestamp / 30;

            // Generate valid codes for the time window
            var currentTotp = totp.ComputeTotp(DateTime.UnixEpoch.AddSeconds(timeWindow * 30));
            // Check if provided code matches any valid code
            bool isValid = model.TotpCode == currentTotp;

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
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl
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

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
                return Ok(new { Status = true, Message = "Password changed successfully" });

            return BadRequest(new { Status = false, Message = "Failed to change password", Errors = result.Errors });
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
                return Ok(new { Status = true, Message = "Avatar updated successfully", AvatarUrl = user.AvatarUrl });

            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Status = false, Message = "Avatar update failed" });
        }
    }
}
