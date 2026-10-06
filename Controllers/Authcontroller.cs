using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TemsGroupProject.DTOs;
using TemsGroupProject.Models;
using TemsGroupProject.Services;

namespace TemsGroupProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;
        private readonly AuditService _auditService; // <-- ADDED

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            EmailService emailService,
            AuditService auditService) // <-- CHANGED: auditService added
        {
            _userManager = userManager;
            _config = config;
            _emailService = emailService;
            _auditService = auditService; // <-- ADDED
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return Unauthorized(new { message = "Invalid email or password." });

            if (await _userManager.IsLockedOutAsync(user))
                return Unauthorized(new { message = "This account has been disabled." });

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
                return Unauthorized(new { message = "Invalid email or password." });

            var otp = Random.Shared.Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(5);
            await _userManager.UpdateAsync(user);

            var sent = await _emailService.SendOtpEmailAsync(user.Email!, otp);
            if (!sent)
            {
                user.OtpCode = null;
                user.OtpExpiresAt = null;
                await _userManager.UpdateAsync(user);

                return StatusCode(503, new
                {
                    message = "We couldn't send your verification code right now. Please try again in a moment."
                });
            }

            return Ok(new { message = "A verification code has been sent to your email. Call /api/auth/verify-otp to finish logging in." });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return Unauthorized(new { message = "Invalid email or code." });

            if (user.OtpCode is null || user.OtpExpiresAt is null || user.OtpExpiresAt < DateTime.UtcNow)
                return BadRequest(new { message = "No active code, or it has expired. Please log in again to request a new one." });

            if (user.OtpCode != dto.Otp)
                return Unauthorized(new { message = "Invalid email or code." });

            user.OtpCode = null;
            user.OtpExpiresAt = null;
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var displayName = GetDisplayName(user);

            // <-- ADDED: start a session row and log the login, so Audit can see it
            var sessionId = await _auditService.StartSessionAsync(user.Id, displayName);
            await _auditService.LogAsync(sessionId, user.Id, displayName, "Login");

            var token = GenerateJwtToken(user, roles, sessionId); // <-- CHANGED: sessionId passed in

            return Ok(new
            {
                token,
                user = new UserResponseDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    StaffId = user.StaffId,
                    Role = roles.FirstOrDefault() ?? string.Empty,
                    DateCreated = user.DateCreated,
                    LastLogin = user.LastLogin,
                    IsDisabled = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow
                }
            });
        }

        // <-- ADDED: closes out the session row and logs "Logout" for the audit
        // trail. Note: this does NOT invalidate the JWT itself — JWTs are
        // stateless, so the token technically still works until it naturally
        // expires. A true "force logout" would need a token blacklist, which
        // isn't built here.
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            var firstName = User.FindFirstValue(ClaimTypes.GivenName);
            var lastName = User.FindFirstValue(ClaimTypes.Surname);
            var displayName = $"{firstName} {lastName}".Trim();
            var sessionIdClaim = User.FindFirstValue("sid");

            if (int.TryParse(sessionIdClaim, out var sessionId))
            {
                await _auditService.EndSessionAsync(sessionId);
                await _auditService.LogAsync(sessionId, userId, displayName, "Logout");
            }

            return Ok(new { message = "Logged out." });
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        private static string GetDisplayName(ApplicationUser user)
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : (user.Email ?? "Unknown");
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles, int sessionId) // <-- CHANGED: sessionId param added
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new("staffId", user.StaffId),
                new("sid", sessionId.ToString()) // <-- ADDED: lets every controller know which session a request belongs to
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}