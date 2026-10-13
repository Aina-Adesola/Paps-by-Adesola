using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PAPPSADMIN.DTOs;
using PAPPSADMIN.Mappers;
using PAPPSADMIN.Models;

namespace PAPPSADMIN.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        // "Peter Obi", or the email if the user has no name saved.
        private static string NameOf(ApplicationUser u)
        {
            var full = $"{u.FirstName} {u.LastName}".Trim();
            return !string.IsNullOrWhiteSpace(full) ? full : (u.Email ?? $"User {u.Id}");
        }

        public async Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto)
        {
            if (!Roles.AssignableRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
                return ApiResponse.Fail<UserResponseDto>(
                    $"Role must be one of: {string.Join(", ", Roles.AssignableRoles)}", 400);

            if (await _userManager.FindByEmailAsync(dto.Email) is not null)
                return ApiResponse.Fail<UserResponseDto>("A user with this email already exists.", 409);

            if (await _userManager.Users.AnyAsync(u => u.StaffId == dto.StaffId))
                return ApiResponse.Fail<UserResponseDto>("A user with this Staff ID already exists.", 409);

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email,
                StaffId = dto.StaffId,
                DateCreated = DateTime.UtcNow
            };

            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(user, dto.Password);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database rejected new user {Email}", dto.Email);
                return ApiResponse.Fail<UserResponseDto>(
                    "The user could not be saved. The email or Staff ID may already be in use.", 409);
            }

            if (!result.Succeeded)
                return ApiResponse.Fail<UserResponseDto>(
                    "Could not create the user.", 400, result.Errors.Select(e => e.Description));

            var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return ApiResponse.Fail<UserResponseDto>(
                    "The role could not be assigned, so the new user was not created.", 500,
                    roleResult.Errors.Select(e => e.Description));
            }

            await _auditService.LogCurrentUserAsync(
                "CreatedUser",
                $"created the user {NameOf(user)} (Staff ID {user.StaffId}, {user.Email}) with the role {dto.Role}");

            return ApiResponse.Ok(user.ToDto(new[] { dto.Role }), "User created successfully.", 201);
        }

        public async Task<ApiResponse<List<UserResponseDto>>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.OrderBy(u => u.Id).ToListAsync();

            var result = new List<UserResponseDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(user.ToDto(roles));
            }

            return ApiResponse.Ok(result, "Users retrieved successfully.");
        }

        public async Task<ApiResponse<UserResponseDto>> GetUserByStaffIdAsync(string staffId)
        {
            if (string.IsNullOrWhiteSpace(staffId))
                return ApiResponse.Fail<UserResponseDto>("Staff ID is required.", 400);

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.StaffId == staffId.Trim());
            if (user is null)
                return ApiResponse.Fail<UserResponseDto>("User not found.", 404);

            var roles = await _userManager.GetRolesAsync(user);
            return ApiResponse.Ok(user.ToDto(roles), "User retrieved successfully.");
        }

        public async Task<ApiResponse<UserResponseDto>> EditUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return ApiResponse.Fail<UserResponseDto>("User not found.", 404);

            if (!string.IsNullOrWhiteSpace(dto.Role) &&
                !Roles.AssignableRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
                return ApiResponse.Fail<UserResponseDto>(
                    $"Role must be one of: {string.Join(", ", Roles.AssignableRoles)}", 400);

            // The name as it was BEFORE this edit, so the audit reads naturally.
            var nameBefore = NameOf(user);
            var changes = new List<string>();

            if (!string.IsNullOrWhiteSpace(dto.FirstName) && dto.FirstName != user.FirstName)
            {
                changes.Add($"first name from '{user.FirstName}' to '{dto.FirstName}'");
                user.FirstName = dto.FirstName;
            }

            if (!string.IsNullOrWhiteSpace(dto.LastName) && dto.LastName != user.LastName)
            {
                changes.Add($"last name from '{user.LastName}' to '{dto.LastName}'");
                user.LastName = dto.LastName;
            }

            if (!string.IsNullOrWhiteSpace(dto.StaffId) && dto.StaffId != user.StaffId)
            {
                changes.Add($"Staff ID from '{user.StaffId}' to '{dto.StaffId}'");
                user.StaffId = dto.StaffId;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                !string.Equals(dto.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _userManager.FindByEmailAsync(dto.Email);
                if (emailTaken is not null && emailTaken.Id != user.Id)
                    return ApiResponse.Fail<UserResponseDto>("Another user already has this email.", 409);

                changes.Add($"email from '{user.Email}' to '{dto.Email}'");
                user.Email = dto.Email;
                user.UserName = dto.Email; // login uses the email as the username
            }

            if (changes.Count > 0)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return ApiResponse.Fail<UserResponseDto>(
                        "Could not update the user.", 400, updateResult.Errors.Select(e => e.Description));
            }

            // ---- role change ----
            string? roleChangeDetails = null;
            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                // Use the official spelling of the role (e.g. "Requester"), whatever case was typed.
                var newRole = Roles.AssignableRoles.First(r => string.Equals(r, dto.Role, StringComparison.OrdinalIgnoreCase));
                var currentRoles = await _userManager.GetRolesAsync(user);
                var alreadyHasIt = currentRoles.Any(r => string.Equals(r, newRole, StringComparison.OrdinalIgnoreCase));

                if (!alreadyHasIt || currentRoles.Count > 1)
                {
                    var oldRoleText = currentRoles.Count > 0 ? string.Join(", ", currentRoles) : "no role";

                    // Add the new role FIRST, so a failure can never leave the user without a role.
                    if (!alreadyHasIt)
                    {
                        var addResult = await _userManager.AddToRoleAsync(user, newRole);
                        if (!addResult.Succeeded)
                            return ApiResponse.Fail<UserResponseDto>(
                                "Could not assign the new role.", 500, addResult.Errors.Select(e => e.Description));
                    }

                    var toRemove = currentRoles
                        .Where(r => !string.Equals(r, newRole, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (toRemove.Count > 0)
                    {
                        var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
                        if (!removeResult.Succeeded)
                            return ApiResponse.Fail<UserResponseDto>(
                                "Could not remove the old role.", 500, removeResult.Errors.Select(e => e.Description));
                    }

                    roleChangeDetails = $"changed {nameBefore}'s role from {oldRoleText} to {newRole}";
                }
            }

            // ---- audit: only log what actually changed ----
            if (changes.Count > 0)
                await _auditService.LogCurrentUserAsync(
                    "EditedUser", $"edited {nameBefore}: changed {string.Join("; ", changes)}");

            if (roleChangeDetails is not null)
                await _auditService.LogCurrentUserAsync("ChangedUserRole", roleChangeDetails);

            var roles = await _userManager.GetRolesAsync(user);
            return ApiResponse.Ok(user.ToDto(roles), "User updated successfully.");
        }

        public async Task<ApiResponse<object>> DisableUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return ApiResponse.Fail("User not found.", 404);

            user.LockoutEnabled = true;
            var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (!lockoutResult.Succeeded)
                return ApiResponse.Fail(
                    "Could not disable the user.", 400, lockoutResult.Errors.Select(e => e.Description));

            await _auditService.LogCurrentUserAsync(
                "DisabledUser", $"disabled the account of {NameOf(user)} (Staff ID {user.StaffId})");
            return ApiResponse.Ok($"User {id} has been disabled.");
        }

        public async Task<ApiResponse<object>> EnableUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return ApiResponse.Fail("User not found.", 404);

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
                return ApiResponse.Fail(
                    "Could not re-enable the user.", 400, result.Errors.Select(e => e.Description));

            await _auditService.LogCurrentUserAsync(
                "EnabledUser", $"re-enabled the account of {NameOf(user)} (Staff ID {user.StaffId})");
            return ApiResponse.Ok($"User {id} has been re-enabled.");
        }
    }
}