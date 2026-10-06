using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TemsGroupProject.DTOs;
using TemsGroupProject.Models;
using TemsGroupProject.Services;

namespace TemsGroupProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.SGC)] // Every endpoint here requires the SGC role
    public class UserManagementController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService; // <-- ADDED

        public UserManagementController(UserManager<ApplicationUser> userManager, AuditService auditService) // <-- CHANGED
        {
            _userManager = userManager;
            _auditService = auditService; // <-- ADDED
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!Roles.AssignableRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = $"Role must be one of: {string.Join(", ", Roles.AssignableRoles)}"
                });
            }

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null)
                return Conflict(new { message = "A user with this email already exists." });

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email,
                StaffId = dto.StaffId,
                DateCreated = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(user, dto.Role);

            await LogAuditAsync("CreatedUser", $"Created user {user.Id} ({user.Email}) with role {dto.Role}"); // <-- ADDED

            var response = new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                StaffId = user.StaffId,
                Role = dto.Role,
                DateCreated = user.DateCreated,
                LastLogin = user.LastLogin,
                IsDisabled = false
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
        }

        // GET api/usermanagement — SGC sees every user in the system, across all roles.
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();

            var result = new List<UserResponseDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserResponseDto
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
                });
            }

            return Ok(result.OrderBy(u => u.Id));
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserResponseDto
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
            });
        }

        // <-- ADDED: SGC can edit an existing user's profile and/or role. Every
        // field in UpdateUserDto is optional — only the ones actually sent get changed.
        [HttpPut("{id:int}")]
        public async Task<IActionResult> EditUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrWhiteSpace(dto.LastName)) user.LastName = dto.LastName;
            if (!string.IsNullOrWhiteSpace(dto.StaffId)) user.StaffId = dto.StaffId;

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                !string.Equals(dto.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _userManager.FindByEmailAsync(dto.Email);
                if (emailTaken is not null && emailTaken.Id != user.Id)
                    return Conflict(new { message = "Another user already has this email." });

                user.Email = dto.Email;
                user.UserName = dto.Email; // keep username in sync, since login uses email as the username
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(new { errors = updateResult.Errors.Select(e => e.Description) });

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                if (!Roles.AssignableRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
                    return BadRequest(new { message = $"Role must be one of: {string.Join(", ", Roles.AssignableRoles)}" });

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            await LogAuditAsync("EditedUser", $"Edited user {user.Id} ({user.Email})");

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new UserResponseDto
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
            });
        }

        [HttpPost("{id:int}/disable")]
        public async Task<IActionResult> DisableUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();

            user.LockoutEnabled = true;
            var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            if (!lockoutResult.Succeeded)
                return BadRequest(new { errors = lockoutResult.Errors.Select(e => e.Description) });

            await LogAuditAsync("DisabledUser", $"Disabled user {id}"); // <-- ADDED

            return Ok(new { message = $"User {id} has been disabled." });
        }

        [HttpPost("{id:int}/enable")]
        public async Task<IActionResult> EnableUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);

            await LogAuditAsync("EnabledUser", $"Enabled user {id}"); // <-- ADDED

            return Ok(new { message = $"User {id} has been re-enabled." });
        }

        // <-- ADDED: small helper so every action above can log itself in one line,
        // tagging the log with whichever SGC user and session made the change.
        private async Task LogAuditAsync(string action, string details)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionIdClaim = User.FindFirstValue("sid");
            var firstName = User.FindFirstValue(ClaimTypes.GivenName);
            var lastName = User.FindFirstValue(ClaimTypes.Surname);

            var userId = int.TryParse(userIdClaim, out var uid) ? uid : 0;
            var sessionId = int.TryParse(sessionIdClaim, out var sid) ? sid : (int?)null;
            var displayName = $"{firstName} {lastName}".Trim();

            await _auditService.LogAsync(sessionId, userId, displayName, action, details);
        }
    }
}